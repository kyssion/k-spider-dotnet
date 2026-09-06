# k-spider-dotnet

7x24 小时金融数据爬虫：抓取东方财富新闻快讯（33 个栏目）与股票 Level1 日线快照，存入 PostgreSQL，支持远端到本地的增量数据同步。

## 功能

- **新闻管线**（三段接力，状态机驱动，失败自动重试）：列表发现 → 原始 JSON 下载 → 结构化解析（段落/图片/列表/表格），按 `news_url` 全局去重。
- **股票管线**：A股（沪/深北）与港股的当日 Level1 归档快照，Cron 工作日收盘后执行，按 `(date, stock_id)` 去重（默认停用，按需启用）。
- **健康检查**：栏目接口可用性探测 + 流水线积压/失败统计（每 5 分钟）。
- **数据搬运**：独立进程 `k-spider-sync` 将远端库的 4 张新闻表增量同步到本地（SqlSugar，单表失败不阻断其余表）。

## 解决方案结构

```
k-spider-dotnet/                 仓库根 = 解决方案根
├── db/                          DDL + 优化 SQL（k-script-spider-datasource.sql / optimization.sql + 手册）
├── deploy/                      systemd 服务模板
├── scripts/                     verify.sh 一键验证
├── .github/workflows/ci.yml     CI（push/PR 构建测试）
├── Directory.Build.props        公共构建属性（net10.0 / Nullable 等）
├── Directory.Packages.props     中央包版本管理（CPM）
└── src/
    ├── k-spider-dotnet/         主爬虫（Host + DI + Quartz 托管调度 , 含 Playwright 特殊页面抓取）
    ├── k-spider-sync/           远端 PG → 本地 PG 增量同步（复用主项目实体）
    └── k-spider-test/           MSTest 单元测试（离线可跑）
```

> .NET 10 / Generic Host + 依赖注入 + Options 模式；ORM 统一 SqlSugar，实体只维护一套（主项目 `Model/`）；命名空间 PascalCase（`KSpider.*`）。

## 快速开始

环境要求：.NET SDK 10、PostgreSQL 15+。

```bash
# 1. 初始化数据库（表结构 + 触发器 + 索引）
psql -h 127.0.0.1 -U postgres -f db/k-script-spider-datasource.sql k_script_spider

# 2. 配置 : 默认 Development 环境连本机 127.0.0.1:5432/k_script_spider
#    编辑 src/k-spider-dotnet/appsettings.Development.json 或用环境变量覆盖

# 3. 构建 + 测试
./scripts/verify.sh

# 4. 运行
dotnet run --project src/k-spider-dotnet
```

## 多环境配置

| 文件 | 环境 | 说明 |
|---|---|---|
| `appsettings.json` | 公共 | 基础默认值 |
| `appsettings.Development.json` | Development（默认） | 本地 PostgreSQL |
| `appsettings.Test.json` | Test | 测试环境 PG（连接串留空，部署注入） |
| `appsettings.Production.json` | Production | 线上 PG（连接串留空，部署注入） |

- **环境选择**：`DOTNET_ENVIRONMENT=Development|Test|Production`（Host 标准，未设置时本地默认 Development）
- **读取优先级**：基础文件 → 环境文件 → `K_SPIDER__` 前缀环境变量（如 `K_SPIDER__DATABASE__CONNECTIONSTRING`）→ 代码默认值
- **打包只带目标环境文件**：`dotnet publish ... -p:SpiderEnvironment=Production`，产物不含其他环境文件与真实凭据
- **凭据红线**：test/prod 连接串不入库，由部署方用环境变量注入

## 定时任务一览

| Job | 间隔 | 说明 | 默认 |
|---|---|---|---|
| `DfNewsListJob` | 2 分钟 | 抓取栏目列表，批量 ON CONFLICT 写入，自适应翻页 | 启用 |
| `DfNewsContentOriginJob` | 3 秒 | 下载文章原始 JSON（FIFO，失败重试 ≤3 次） | 启用 |
| `DfNewsContentJob` | 1 分钟 | 解析原始 JSON 为结构化内容（失败重试 ≤3 次） | 启用 |
| `DfCheckJob` | 5 分钟 | 栏目接口探测 + 流水线积压统计 | 启用 |
| `StockCnJob` / `StockHkJob` | Cron 工作日 20:00 | 股票 Level1 日线归档 | 停用 |
| `TransferSpiderDataJob`（sync） | 2 分钟 | 远端 → 本地增量同步 | 启用 |

任务的启用/停用：`src/k-spider-dotnet/Program.cs` 的 `AddSpiderJobs` 中注释控制。Ctrl+C / SIGTERM 触发优雅停机（等待在跑任务完成）。

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

状态机：`0 未下载 → 3 已下载原始 → 1 已解析详情`，失败态 `2 / 4` 在 `fail_count < 3` 时自动重试。8 张表完整 DDL 见 [db/k-script-spider-datasource.sql](db/k-script-spider-datasource.sql)。

## 部署（Linux）

```bash
dotnet publish src/k-spider-dotnet/k-spider-dotnet.csproj -c Release -r linux-x64 --self-contained -p:SpiderEnvironment=Production
```

推荐 **systemd**（自动重启 + journald 日志轮转）：模板见 [deploy/k-spider-dotnet.service](deploy/k-spider-dotnet.service)，
拷贝到 `/etc/systemd/system/` 后 `systemctl enable --now k-spider-dotnet`。轻量场景可用产物目录内的 `run.sh`。

## AI 辅助开发

- **[AGENTS.md](AGENTS.md)**：AI 代理操作手册（结构、命令、约定、扩展套路、已知坑），ZCode / Claude Code / Cursor 自动读取。
- **离线测试**：`dotnet test src/k-spider-test/k-spider-test.csproj` 不依赖网络与数据库。
- **一键验证**：`./scripts/verify.sh` = 构建 + 测试（CI 同款）。
