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
| `K_SPIDER__DATABASE__CONNECTIONSTRING` | 主程序 / Web 控制台 PG 连接串 | 映射到 `Database:ConnectionString`（双下划线是层级分隔符）；`k-spider-web` 与主程序连同一个库 |
| `K_SPIDER_REMOTE__CONNECTIONSTRING` | `k-spider-sync` 远端库 | **前缀是单下划线 `K_SPIDER_`**，与主程序不一致，照抄主程序的写法会静默失效 |
| `K_SPIDER_LOCAL__CONNECTIONSTRING` | `k-spider-sync` 本地库 | 同上 |
| `ASPNETCORE_URLS` | `k-spider-web` 监听地址 | 默认 `http://localhost:5800`（appsettings.json 的 `Urls`）；需要外部访问时覆盖或用 nginx 反代 |

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

- 模板已设 `TZ=Asia/Shanghai`（快讯滞后计算等"当日/当前时刻"判断都依赖时区，UTC 机器上会算错）。
- 拷贝到 `/etc/systemd/system/` 后 `systemctl enable --now k-spider-dotnet`。
- 轻量场景可直接用产物目录内的 `run.sh`。

**Chromium（反爬验证用，可选但推荐装）**：正常抓取是纯 HTTP，只有被 JS / Cloudflare 类挑战拦住时
`BrowserChallengeSolver` 才会启动 Chromium 过验证。未安装时该策略会失败并在日志里提示，
识别与告警仍然生效，抓取链路不受影响。安装：

```bash
dotnet build src/k-spider-dotnet        # 让 Playwright 的构建目标就位
pwsh src/k-spider-dotnet/bin/Debug/net10.0/playwright.ps1 install chromium --with-deps
```

自包含发布产物同理（用产物目录里的 `playwright.ps1`，或在同版本 SDK 环境执行一次后把浏览器缓存目录带到目标机）。
Linux 上 `--with-deps` 需要 root，装完浏览器缓存在 `~/.cache/ms-playwright`。

`k-spider-sync` 是独立进程，单独发布与部署（`src/k-spider-sync/k-spider-sync.csproj`），与主爬虫互不影响。

### Web 控制台（`k-spider-web`）

```bash
# 1. 构建前端 ( 需要 Node 20+ 与 pnpm ; 产物拷入 k-spider-web/wwwroot )
bash scripts/build-web.sh

# 2. 自包含发布 ( 产物含前端静态页 )
dotnet publish src/k-spider-web/k-spider-web.csproj \
    -c Release -r linux-x64 --self-contained -p:SpiderEnvironment=Production
```

- systemd 模板：`deploy/k-spider-web.service`（与主爬虫分属两个服务，互不影响重启）。
- 数据库连接串用 `K_SPIDER__DATABASE__CONNECTIONSTRING` 注入，与主程序同库；只读查询 + 写指令表，不写新闻表。
- 默认只监听 `localhost:5800`；远程访问推荐 nginx 反代（可在这层加 Basic Auth）：

  ```nginx
  location / {
      proxy_pass http://127.0.0.1:5800;
      proxy_set_header Host $host;
  }
  ```

- 健康探针：`GET /api/health`；开发期可在 `http://localhost:5800/openapi/v1.json` 拿 OpenAPI 文档（仅 Development 环境）。
- 前端本地开发：`cd web && pnpm install && pnpm dev`（5173 端口，`/api` 自动代理到 5800）。

## 三、监控与巡检

- **日志是主要的监控手段**：`NewsCheckJob` 每 5 分钟输出（a）逐源逐栏目接口探测结果，空数据记错误日志（含研报列表接口）；（b）分源各状态数量与全库最老未处理新闻时间；（c）处于反爬验证冷却期的源（识别到验证但自动通过失败），有则逐条告警；（d）研报实时性（最新一篇发布日期距今，滞后超 3 天告警）、盘面榜单实时性（最新交易日距今超 5 天告警）与公告实时性（最新披露时间滞后超 1 天告警）。另有 `VerificationPipeline` 在识别到验证、策略未通过、进入冷却时各记一条日志。
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

-- 研报回填积压 ( summary 长期为空说明详情页模板改版 )
SELECT count(*) FROM spider_research_report WHERE summary IS NULL AND summary_fail_count < 3;

-- 研报实时性 ( 最新一篇距今天数 , 每日都有发布 , 超过 3 天为异常 )
SELECT max(publish_date) FROM spider_research_report;

-- 盘面榜单实时性与各类型行数 ( 每日披露 , 超过 5 天为异常 ; 北向无每日披露属预期 )
SELECT ranking_type, count(*), max(trade_date) FROM spider_ranking GROUP BY 1 ORDER BY 1;

-- 公告实时性 ( 工作日高频披露 , 滞后超 1 天为异常 )
SELECT count(*), max(publish_time) FROM spider_announcement;

-- 公告样例 : 最新 10 条 ( 分类代码对照巨潮分类表 )
SELECT sec_code, sec_name, title, pdf_url FROM spider_announcement ORDER BY publish_time DESC LIMIT 10;

-- 盘面榜单样例 : 某日龙虎榜净买入前 10
SELECT stock_code, stock_name, deal_amount, net_amount, detail->>'EXPLAIN' AS 席位解释
FROM spider_ranking WHERE ranking_type = 1 AND trade_date = (SELECT max(trade_date) FROM spider_ranking)
ORDER BY net_amount DESC LIMIT 10;
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
| 重复行 | 查 `news_url` 唯一键是否仍在 | 三张新闻表的去重都依赖唯一键，重建表时要带上约束 |
| 日志出现 `VerificationRequiredException` / `[ManualEscalationSolver]` | 看日志里的 `kind` 与 `依据`（命中的特征片段） | 按验证类型处置：JS 门禁 / Cloudflare → 确认 Chromium 是否装了（`playwright install chromium`）；滑块 → 确认该源是否已按 [anti-bot-verification.md](anti-bot-verification.md) 放开；图形 / 短信验证码 → 只能人工；`RiskControl` / `RateLimited` → 属退避类，等冷却结束自动重试，持续出现再核对请求头与签名参数是否随源站前端版本变化 |
| `NewsCheckJob` 报某 host 处于验证冷却期 | 看 `[VerificationPipeline]` 的识别与策略失败日志 | 冷却期内该源不会重试（默认 10 分钟；限流按源站 `Retry-After` 退避），期间数据按失败计数；等冷却结束自动恢复，或人工确认后重启进程清掉会话缓存与冷却状态 |
| 浏览器策略报 `Executable doesn't exist` | 目标机没装 Chromium | 按本文"二、部署"的 Chromium 小节安装；不装也能跑，只是 JS 类挑战过不去 |

## 五、数据同步（`k-spider-sync`）

每 2 分钟一轮，把远端库的 8 张表（4 张网页新闻 + `spider_flash_news` + `spider_research_report` + `spider_ranking` + `spider_announcement`）搬到本地库，`DisallowConcurrentExecution` + 优雅停机，与主爬虫互不影响。本地库缺表时先执行主 DDL 对应段（同步进程不做建表）。

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
