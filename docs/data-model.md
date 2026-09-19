# 数据模型

库名 `k_script_spider`，9 张表（4 张网页新闻 + 1 张实时快讯 + 4 张股票）。完整 DDL（pg_dump 导出 + 增量演进段）在
[`db/k_script_spider.sql`](../db/k_script_spider.sql)，新环境用它初始化。

## 一、表清单

| 表 | 职责 | 唯一键 | 写入方 |
|---|---|---|---|
| `spider_news_list` | 新闻列表与流水线状态（`download_status_code` / `fail_count` / `from_media` / `category`） | `news_url` | `NewsListJob` |
| `spider_news_content_origin` | 原始响应（整篇 JSON / HTML），解析可重跑的底料 | `news_url` | `NewsContentOriginJob` / `NewsListJob`（快讯型源） |
| `spider_news_content` | 结构化详情：`news_content_json`（片段数组）+ `news_content_text` + `news_keyword` | `news_url` | `NewsContentJob` |
| `spider_news_image_list` | 正文图片地址与文件名 | `image_resource_url` | `NewsContentJob` |
| `spider_flash_news` | 实时快讯（列表即全文，拉到即终态：标题/正文/标签/重要度 1-3/关联标的/图片/原始 JSON） | `(from_media, news_url)` | `FlashNewsJob` |
| `stock_cn_introduction` | 股票池（A 股 + 港股共用，`exchange_channel` 区分市场） | `stock_id` | 外部导入 |
| `stock_cn_level1_archived_daily_origin` | A 股当日 Level1 归档快照 | `(date, stock_id)` | `StockCnJob` |
| `stock_hk_level1_archived_daily_origin` | 港股当日 Level1 归档快照 | `(date, stock_id)` | `StockHkJob` |
| `stock_usa_level1_archived_daliy_origin` | 美股（未接入，表名 `daliy` 为历史笔误，已成契约） | `(date, stock_id)` | — |

另有 `sync_transfer_watermark`（**只存在于本地同步库**，由 `k-spider-sync` 启动时幂等创建，不在主库 DDL 里）：记录每张表已同步到的 `(update_time, id)` 水位。

## 二、唯一键与去重语义

- 网页新闻三表都以 `news_url` 去重，图片表以 `image_resource_url`，**跨源全局去重**（同一 URL 只落一次）；快讯表以 `(from_media, news_url)` 去重，各源独立命名空间（与"media 相互独立"的设计一致）。
- 股票日线以 `(date, stock_id)` 去重，同日重跑幂等。
- 已知冗余：A 股 / 港股日线表各有一对方向相反的等价唯一索引（`UNIQUE(date, stock_id)` 约束 + `UNIQUE(stock_id, date)` 索引），属历史遗留、保留不动。
- 约束改名：`spider_news_content` 上的约束名仍是调试期残留的 `uk_news_content_test_url`，如需改名执行 `ALTER TABLE public.spider_news_content RENAME CONSTRAINT uk_news_content_test_url TO uk_news_content_url;`（纯改名，不影响业务）。

## 三、索引与热路径

- 轮询热路径索引（启动时由 `Pg.EnsureSpiderNewsListDbObjects()` 幂等创建）：

  ```sql
  CREATE INDEX IF NOT EXISTS idx_news_list_download_status
      ON public.spider_news_list (download_status_code, id)
      WHERE download_status_code IN (0, 2, 3, 4);
  ```

  两个消费任务都是"按状态过滤 + 按 id 升序取 N 条"，这个部分索引正好覆盖；
  因为查询**不带 `from_media` 条件**（全源统一 FIFO），所以不需要把来源列放进索引前导位。

- 升级索引必须用 `CREATE INDEX CONCURRENTLY`，否则建索引期间的写锁会卡住 3 秒轮询的下载任务；CONCURRENTLY 不能在事务块内执行，失败会留下 INVALID 索引，删掉重跑即可。

## 四、时间戳与触发器

所有表都有 `update_time_func()` 触发器自动刷新 `update_time`。因此：

- upsert 时忽略 `id` / `create_time` / `update_time` 三列，让库自己维护。
- 本地同步时也忽略时间戳列（本地触发器会重新打时间），只比远端的时间戳推进水位。

## 五、迁移约定

表**不是** CodeFirst 管理，实体由 DbFirst 反向生成：

| 变更类型 | 做法 |
|---|---|
| 新增列 / 索引（增量演进） | 加到 `Pg.EnsureSpiderNewsListDbObjects()`，启动时幂等执行（示例：`fail_count` 列、轮询部分索引） |
| 结构性变更（新表、改约束、改索引语义） | 同时改 `Model/` 实体与 `db/k_script_spider.sql` |
| 重新生成实体 | `Data/Devtools/PgDevelop.cs`（开发期工具，跑一次按库反向生成） |

**改表要改三处**：`Model/` 实体、DDL 文件、必要的增量演进代码；漏掉 DDL 会让新环境初始化失败，漏掉 `Model/` 会让代码编译不过。

## 六、数据治理

- `spider_news_content_origin` 与快讯表的 `raw_content` 存原始响应、图片表只增不删，长期运行会持续增长，需要定期归档清理（暂无自动保留策略，原治理脚本已移除，可从 git 历史找回）。
- 图片只记录 URL 不下载内容，因此图片表体积可控。
- 远端库到本地库的同步只搬新闻 4 表；股票表留在远端（本地分析侧暂时不需要）。
