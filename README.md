# k-spider-dotnet

7x24 小时金融数据爬虫：抓取东方财富新闻快讯（33 个栏目）与股票 Level1 日线快照，存入 PostgreSQL，支持远端到本地的增量数据同步。

## 功能

- **新闻管线**（三段接力，状态机驱动，失败自动重试）：列表发现 → 原始 JSON 下载 → 结构化解析（段落/图片/列表/表格），按 `news_url` 全局去重。
- **股票管线**：A股（沪/深北）与港股的当日 Level1 归档快照，Cron 工作日收盘后执行，按 `(date, stock_id)` 去重。
- **健康检查**：栏目接口可用性探测 + 流水线积压/失败统计（每 5 分钟）。
- **数据搬运**：独立进程 `k-spider-sync` 将远端库的 4 张新闻表增量同步到本地（SqlSugar，单表失败不阻断其余表）。

## 解决方案结构

```
k-spider-dotnet/              仓库根 = 解决方案根
├── db/                       数据库 DDL（k-script-spider-datasource.sql）
├── scripts/                  verify.sh 一键验证
└── src/
    ├── k-spider-dotnet/      主爬虫（Quartz 调度 + SqlSugar 落库 , 含 Playwright 特殊页面抓取）
    ├── k-spider-dotnet-lib/  公共库（SpiderJob 抽象 / 日志 / 飞书 SDK / 工具类）
    ├── k-spider-sync/        远端 PG → 本地 PG 增量同步（SqlSugar , 复用主项目实体）
    └── k-spider-test/        MSTest 单元测试（离线可跑）
```

> 全项目统一使用 SqlSugar ORM，实体只维护一套（主项目 `src/k-spider-dotnet/model/`）。

## 快速开始

环境要求：.NET SDK 10、PostgreSQL 15+。

```bash
# 1. 初始化数据库（表结构 + 触发器 + 索引）
psql -h 127.0.0.1 -U postgres -f db/k-script-spider-datasource.sql k_script_spider

# 2. 配置（默认连接 127.0.0.1:5432/k_script_spider，按需修改）
#    方式一：编辑 src/k-spider-dotnet/appsettings.json
#    方式二：环境变量，如 K_SPIDER__DATABASE__CONNECTIONSTRING=...

# 3. 构建 + 测试
./scripts/verify.sh

# 4. 运行
dotnet run --project src/k-spider-dotnet
```

配置读取优先级：`appsettings.json` → `K_SPIDER_` 前缀环境变量（层级用 `__`）→ 代码内默认值。

| 配置键 | 环境变量 | 说明 |
|---|---|---|
| `Database:ConnectionString` | `K_SPIDER__DATABASE__CONNECTIONSTRING` | 主程序 PostgreSQL 连接串 |
| —（仅环境变量） | `K_SPIDER_REMOTE__CONNECTIONSTRING` | k-spider-sync 远端库连接串 |
| —（仅环境变量） | `K_SPIDER_LOCAL__CONNECTIONSTRING` | k-spider-sync 本地库连接串 |

老库升级：程序启动时会自动幂等补齐 `spider_news_list.fail_count` 列与轮询部分索引，无需手工执行 SQL。

## 定时任务一览

| Job | 间隔 | 说明 | 默认 |
|---|---|---|---|
| `DfNewsListJob` | 2 分钟 | 抓取栏目列表，批量 ON CONFLICT 写入，自适应翻页 | 启用 |
| `DfNewsContentOriginJob` | 3 秒 | 下载文章原始 JSON（FIFO，失败重试 ≤3 次） | 启用 |
| `DfNewsContentJob` | 1 分钟 | 解析原始 JSON 为结构化内容（失败重试 ≤3 次） | 启用 |
| `DfCheckJob` | 5 分钟 | 栏目接口探测 + 流水线积压统计 | 启用 |
| `StockCnJob` / `StockHkJob` | Cron 工作日 20:00 | 股票 Level1 日线归档 | 停用 |
| `TransferSpiderDataJob`（sync） | 2 分钟 | 远端 → 本地增量同步 | 启用 |

任务的启用/停用通过 `src/k-spider-dotnet/Program.cs` 中注释控制。Ctrl+C 触发优雅停机（等待在跑任务完成）。

## 数据流

```
东方财富列表 API ──DfNewsListJob──▶ spider_news_list (status=0)
                                        │ DfNewsContentOriginJob (0→3 , 失败→4 可重试)
                                        ▼
                                  spider_news_content_origin (原始 JSON)
                                        │ DfNewsContentJob (3→1 , 失败→2 可重试)
                                        ▼
                              spider_news_content (结构化片段+纯文本)
                              spider_news_image_list (图片 URL)
```

状态机：`0 未下载 → 3 已下载原始 → 1 已解析详情`，失败态 `2 解析失败 / 4 下载失败`。失败态在 `fail_count < 3` 时会被对应 Job 重新选中重试；解析阶段发现原始内容缺失会自动退回 `4` 触发重新下载。

8 张表的完整 DDL 见 [db/k-script-spider-datasource.sql](db/k-script-spider-datasource.sql)。

## AI 辅助开发

本仓库已做 AI 编码代理友好化改造：

- **[AGENTS.md](AGENTS.md)**：AI 代理操作手册（项目结构、命令、代码约定、扩展点套路、已知坑），ZCode / Claude Code / Cursor 等代理会自动读取。
- **离线测试**：`dotnet test src/k-spider-test/k-spider-test.csproj` 不依赖网络与数据库，覆盖工具类与新闻解析核心逻辑，改完代码可随时验证。
- **一键验证**：`./scripts/verify.sh` = 构建 + 测试。
- **配置集中**：连接串统一走 `AppConfig`（`appsettings.json` / 环境变量 / 代码默认值三级回退）。

## 部署（Linux）

```bash
dotnet publish src/k-spider-dotnet/k-spider-dotnet.csproj -c Release -r linux-x64 --self-contained
# 产物目录内执行
./run.sh   # nohup 后台运行, 日志写入 output.log
```
