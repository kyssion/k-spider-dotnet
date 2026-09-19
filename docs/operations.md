# 运行与运维

## 一、配置

### 环境选择与优先级

环境由 `DOTNET_ENVIRONMENT` 决定（`Development` / `Test` / `Production`；`Program.Main` 在未设置时默认 `Development`，因为 Host 自身默认是 `Production`）。

读取优先级（后者覆盖前者）：

```
appsettings.json  →  appsettings.{环境}.json  →  K_SPIDER__ 前缀环境变量  →  代码内默认值
```

### 环境变量

| 变量 | 用途 | 说明 |
|---|---|---|
| `K_SPIDER__DATABASE__CONNECTIONSTRING` | 主程序 PG 连接串 | 映射到 `Database:ConnectionString`（双下划线是层级分隔符） |
| `K_SPIDER_REMOTE__CONNECTIONSTRING` | `k-spider-sync` 远端库 | **前缀是单下划线 `K_SPIDER_`**，与主程序不一致，照抄主程序的写法会静默失效 |
| `K_SPIDER_LOCAL__CONNECTIONSTRING` | `k-spider-sync` 本地库 | 同上 |

配置值为空 = 使用代码内默认值（本地开发库）。

### 凭据红线

- **test / prod 连接串不入库**，由部署方用环境变量注入；发布产物只携带目标环境配置文件（`-p:SpiderEnvironment=Production`）。
- 现状提醒：`appsettings.Development.json` 与 `TransferConfig` 的默认值里保留着内网 / 本地明文连接串（历史遗留）。生产部署必须用环境变量覆盖，条件允许时应清理这些默认值。

## 二、部署（Linux）

```bash
# 自包含发布 : 产物只带目标环境配置 , 不依赖目标机安装 SDK
dotnet publish src/k-spider-dotnet/k-spider-dotnet.csproj \
    -c Release -r linux-x64 --self-contained -p:SpiderEnvironment=Production
```

产物用 `deploy/` 下的 systemd 模板托管：

- 模板已设 `TZ=Asia/Shanghai`（股票任务与"当日"判断都依赖时区，UTC 机器上日期会错）。
- 拷贝到 `/etc/systemd/system/` 后 `systemctl enable --now k-spider-dotnet`。
- 轻量场景可直接用产物目录内的 `run.sh`。

`k-spider-sync` 是独立进程，单独发布与部署（`src/k-spider-sync/k-spider-sync.csproj`），与主爬虫互不影响。

## 三、监控与巡检

- **日志是主要的监控手段**：`NewsCheckJob` 每 5 分钟输出（a）逐源逐栏目接口探测结果，空数据记错误日志；（b）分源各状态数量与全库最老未处理新闻时间。
- **启动自检**：`Pg.EnsureSpiderNewsListDbObjects()` 除补齐列与索引外，还会检查批量 upsert 依赖的唯一约束是否齐全（`CheckBatchUpsertUniqueIndexes`）。缺约束时打印明确错误（表名 + 列名），因为这种缺失会让"列表即全文"型源整批写入失败、且不影响其它源，从数据现象上极难定位。
- 目前**没有**指标上报与告警通道（飞书 SDK 保留在 `Lark/` 但无调用方）。判断系统是否健康靠以下 SQL 与日志：

```sql
-- 分源积压概况
SELECT from_media, download_status_code, count(*) FROM spider_news_list GROUP BY 1, 2 ORDER BY 1, 2;

-- 各源最新数据时间 ( 判断某源是否已停止入库 )
SELECT from_media, count(*), max(news_time) FROM spider_news_list GROUP BY 1 ORDER BY 1;

-- 批量 upsert 依赖的唯一约束清单 ( 四张表都应在列 )
SELECT t.relname AS 表, a.attname AS 唯一列
FROM pg_index i
JOIN pg_class t ON t.oid = i.indrelid
JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY (i.indkey)
WHERE i.indisunique
  AND t.relname IN ('spider_news_list', 'spider_news_content_origin',
                    'spider_news_content', 'spider_news_image_list')
ORDER BY 1, 2;

-- 悬空行 ( 已置为已下载却没有 origin ) , 期望 0
SELECT count(*) FROM spider_news_list l
LEFT JOIN spider_news_content_origin o ON o.news_url = l.news_url
WHERE l.download_status_code IN (3, 1) AND o.id IS NULL;

-- 达到重试上限的终态行 ( 需要人工判断是源改版还是脏数据 )
SELECT from_media, download_status_code, count(*) FROM spider_news_list
WHERE fail_count >= 3 AND download_status_code IN (2, 4) GROUP BY 1, 2;

-- 最老待处理新闻 ( 判断积压是否在推进 )
SELECT min(news_time) FROM spider_news_list WHERE download_status_code = 0;
```

## 四、排障手册

| 症状 | 排查步骤 | 处置 |
|---|---|---|
| **某个源一行数据都没有，其它源正常** | 查该源是否"列表即全文"型（会写 origin），再查 origin 表的唯一约束是否还在：<br>`SELECT t.relname, a.attname FROM pg_index i JOIN pg_class t ON t.oid=i.indrelid JOIN pg_attribute a ON a.attrelid=t.oid AND a.attnum=ANY(i.indkey) WHERE i.indisunique AND t.relname IN ('spider_news_list','spider_news_content_origin','spider_news_content','spider_news_image_list');` | 批量 upsert 用 `ON CONFLICT (列)`，该列缺唯一约束时 PostgreSQL 整批报错；列表行与 origin 同事务，异常会把该源整批写入回滚。按 `db/k_script_spider.sql` 补建唯一约束即可恢复（进程启动时的 `CheckBatchUpsertUniqueIndexes` 会显式告警，见下） |
| 某个源完全没有新新闻 | 1. 跑连通性用例 `dotnet test ... --filter "TestCategory=Live"`；2. 看 `NewsCheckJob` 该源的栏目探测日志 | 接口能连上却拿不到数据 → 源改版，按 [news-pipeline.md](news-pipeline.md) 的源明细核对参数与解析规则；网络不通 → 先解决出口网络 |
| 财联社报 `errno 10012 签名错误` | 看 `NewsCheckJob` 日志是否有 10012 | 前端版本号变更，更新 `ClsNewsResource.Sv` 并跑 `SignMatchesVerifiedVector` 用例核对算法 |
| 财联社突然返回空数组（errno 仍为 0） | 检查请求参数里的 `rn` | `rn > 50` 会被静默返回空，`MaxPageSize` 已钳制，确认没被改大 |
| 待处理数量持续上涨 | 查分源状态计数与最老待处理时间 | 若 `status = 0/4` 堆积在某个源：该源接口异常；若 `status = 2` 堆积：解析规则与源改版不匹配，改完解析后**直接用已存的 origin 重跑**（不必重抓） |
| 解析失败突然增多 | 抽样看 `spider_news_content_origin` 里的原始内容 | 源页面结构变更 → 更新解析规则；改完把对应行的 `download_status_code` 置回 `3` 即可重跑解析 |
| 出现大量 `KDbException` / 连接异常 | 检查数据库可用性与连接串 | 数据库抖动不会消耗重试次数，恢复后自动续跑；本地无 PG 时各 Job 每轮抛异常属预期噪音 |
| 股票数据日期不对 | 确认进程时区 | systemd 模板已设 `TZ=Asia/Shanghai`；自管进程需自行设置 |
| 重复行 | 查 `news_url` 唯一键是否仍在 | 三张新闻表的去重都依赖唯一键，重建表时要带上约束 |

## 五、数据同步（`k-spider-sync`）

每 2 分钟一轮，把远端库的 4 张新闻表搬到本地库，`DisallowConcurrentExecution` + 优雅停机，与主爬虫互不影响。

**两条同步通道**：

| 通道 | 依据 | 行为 |
|---|---|---|
| 新行 | 本地最大 `Id` 之后按 `Id` 分批拉取（每批 2000） | 批内过滤掉本地已存在的 Id 后插入；单条坏数据不中断整表 |
| 已有行更新 | `(update_time, id)` **双键水位** | 让远端的状态流转（如 `spider_news_list` 的 `0→3→1`）与内容修正传播到本地 |

**水位语义**（`sync_transfer_watermark` 表，只存在于本地库）：

- 首次运行没有水位时，以远端当前最大 `update_time` 为起点，**只跟踪此后的更新**（存量差异不回补）。
- 每批同步后立刻持久化水位，中途失败下一轮从最近水位续拉，天然幂等；`update_time` 相同的行被批次截断时靠 `id` 继续推进，不会漏行。
- 本地不存在的 Id 不做兜底插入（避免与"新行通道"冲突产生重复），新行一律由 `Id` 通道负责。
- 更新时忽略时间戳列，本地 `update_time` 由本地触发器重新维护。

**注意**：同步只搬运数据，**不传播 DELETE**。远端删除行不会让本地删行；治理类 SQL（清理、约束变更）需要在两边各自执行一遍。
