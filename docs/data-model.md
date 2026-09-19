# 数据模型

库名 `k_script_spider`，8 张表（4 张新闻 + 4 张股票）。完整 DDL（pg_dump 导出 + 增量演进段）在
[`db/k-script-spider-datasource.sql`](../db/k-script-spider-datasource.sql)，新环境用它初始化。

## 一、表清单

| 表 | 职责 | 唯一键 | 写入方 |
|---|---|---|---|
| `spider_news_list` | 新闻列表与流水线状态（`download_status_code` / `fail_count` / `from_media` / `category`） | `news_url` | `NewsListJob` |
| `spider_news_content_origin` | 原始响应（整篇 JSON / HTML），解析可重跑的底料 | `news_url` | `NewsContentOriginJob` / `NewsListJob`（快讯型源） |
| `spider_news_content` | 结构化详情：`news_content_json`（片段数组）+ `news_content_text` + `news_keyword` | `news_url` | `NewsContentJob` |
| `spider_news_image_list` | 正文图片地址与文件名 | `image_resource_url` | `NewsContentJob` |
| `stock_cn_introduction` | 股票池（A 股 + 港股共用，`exchange_channel` 区分市场） | `stock_id` | 外部导入 |
| `stock_cn_level1_archived_daily_origin` | A 股当日 Level1 归档快照 | `(date, stock_id)` | `StockCnJob` |
| `stock_hk_level1_archived_daily_origin` | 港股当日 Level1 归档快照 | `(date, stock_id)` | `StockHkJob` |
| `stock_usa_level1_archived_daliy_origin` | 美股（未接入，表名 `daliy` 为历史笔误，已成契约） | `(date, stock_id)` | — |

另有 `sync_transfer_watermark`（**只存在于本地同步库**，由 `k-spider-sync` 启动时幂等创建，不在主库 DDL 里）：记录每张表已同步到的 `(update_time, id)` 水位。

## 二、唯一键与去重语义

- 新闻三表都以 `news_url` 去重，图片表以 `image_resource_url`；**跨源全局去重**（同一 URL 只落一次）。
- 股票日线以 `(date, stock_id)` 去重，同日重跑幂等。
- 已知冗余：A 股 / 港股日线表各有一对方向相反的等价唯一索引（`UNIQUE(date, stock_id)` 约束 + `UNIQUE(stock_id, date)` 索引），删除脚本与回滚方式见 `db/optimization.md` 第 2.1 节。
- 约束改名：`spider_news_content` 上的 `uk_news_content_test_url`（调试期残留的 "test"）改名为 `uk_news_content_url` 的脚本见 `db/optimization.md` 第 2.3 节。

## 三、索引与热路径

- 轮询热路径索引（启动时由 `Pg.EnsureSpiderNewsListDbObjects()` 幂等创建）：

  ```sql
  CREATE INDEX IF NOT EXISTS idx_news_list_download_status
      ON public.spider_news_list (download_status_code, id)
      WHERE download_status_code IN (0, 2, 3, 4);
  ```

  两个消费任务都是"按状态过滤 + 按 id 升序取 N 条"，这个部分索引正好覆盖；
  因为查询**不带 `from_media` 条件**（全源统一 FIFO），所以不需要把来源列放进索引前导位。

- 升级索引（例如为 `NewsCheckJob` 的 `MIN(news_time)` 加 `INCLUDE`）必须用 `CREATE INDEX CONCURRENTLY`，否则建索引期间的写锁会卡住 3 秒轮询的下载任务，详见 `db/optimization.md` 第 2.2 节。

## 四、时间戳与触发器

所有表都有 `update_time_func()` 触发器自动刷新 `update_time`。因此：

- upsert 时忽略 `id` / `create_time` / `update_time` 三列，让库自己维护。
- 本地同步时也忽略时间戳列（本地触发器会重新打时间），只比远端的时间戳推进水位。

## 五、迁移约定

表**不是** CodeFirst 管理，实体由 DbFirst 反向生成：

| 变更类型 | 做法 |
|---|---|
| 新增列 / 索引（增量演进） | 加到 `Pg.EnsureSpiderNewsListDbObjects()`，启动时幂等执行（示例：`fail_count` 列、轮询部分索引） |
| 结构性变更（新表、改约束、改索引语义） | 同时改 `Model/` 实体与 `db/k-script-spider-datasource.sql`，并按需在 `db/optimization.md` 记录上线脚本 |
| 重新生成实体 | `Data/Devtools/PgDevelop.cs`（开发期工具，跑一次按库反向生成） |

**改表要改三处**：`Model/` 实体、DDL 文件、必要的增量演进代码；漏掉 DDL 会让新环境初始化失败，漏掉 `Model/` 会让代码编译不过。

## 六、数据治理

- `spider_news_content_origin` 存整篇原始响应，`spider_news_image_list` 只增不删，长期运行会持续增长；定期治理用 `db/optimization.sql` 的清理段（暂无自动保留策略）。
- 图片只记录 URL 不下载内容，因此图片表体积可控。
- 远端库到本地库的同步只搬新闻 4 表；股票表留在远端（本地分析侧暂时不需要）。
