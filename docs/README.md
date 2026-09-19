# docs — 系统设计文档

本目录面向**维护者**，记录这套爬虫系统的设计原则、架构、现状与运维手册。

## 与其他文档的分工

| 文档 | 面向 | 内容 |
|---|---|---|
| [README.md](../README.md) | 使用者 / 新人 | 这是什么、怎么跑起来、快速开始 |
| [AGENTS.md](../AGENTS.md) | AI 编码代理 | 操作手册：命令、约定、扩展套路、已知坑（工具性、可执行） |
| `docs/`（本目录） | 维护者 | 设计文档：为什么这么设计、系统全貌、演进与取舍 |

三者都描述同一个系统，**结构或约定变更时必须同步更新**（见下方映射表）。

## 文档索引

| 文档 | 内容 |
|---|---|
| [principles.md](principles.md) | 设计原则与工程约定：分层、数据访问、错误处理与重试、测试分层、命名 |
| [architecture.md](architecture.md) | 系统架构：项目分层、运行时模型、模块职责、关键设计决策、扩展点 |
| [news-pipeline.md](news-pipeline.md) | 新闻管线：状态机、多源抽象、已接入源明细、落库与去重、已知限制 |
| [stock-pipeline.md](stock-pipeline.md) | 股票管线：股票池、Level1 归档抓取、停用状态与启用方式 |
| [data-model.md](data-model.md) | 数据模型：8 张表的职责、唯一键、索引、迁移与数据治理 |
| [operations.md](operations.md) | 运行运维：配置、部署、监控、排障手册、数据同步 |

## 文档与代码同步规则

**改了代码就要改对应的文档**，否则文档会变成误导后来者的负担。变更与文档的对应关系：

| 代码变更 | 必须更新 |
|---|---|
| 新增 / 修改新闻源（`Spider/*/`） | [news-pipeline.md](news-pipeline.md) 的源明细表；AGENTS.md 的新闻源套路 |
| 新增 / 修改定时任务（`Job/`、`Program.AddSpiderJobs`） | [architecture.md](architecture.md) 任务表、[operations.md](operations.md) 任务清单、README 任务表 |
| 表结构 / 索引 / 唯一键变更（`Model/`、`db/`、`Pg.EnsureSpiderNewsListDbObjects`） | [data-model.md](data-model.md)、`db/k_script_spider.sql` |
| 状态机 / 重试语义 / 落库语义变更 | [news-pipeline.md](news-pipeline.md) |
| 配置键 / 环境变量 / 部署方式变更 | [operations.md](operations.md)、README 配置章节 |
| 分层、约定、测试策略变更 | [principles.md](principles.md)、AGENTS.md 代码约定 |
| 任何结构性变更 | README.md 结构章节（如目录树、项目职责） |

维护要求：

- 机械兜底：`scripts/check-docs.sh`（已接入 `verify.sh` 与 CI）会校验全部 Markdown 的相对链接，改了文件名或挪了目录却漏改引用时门禁会红；**内容**层面的代码-文档一致性仍靠上表人工保证。
- 文档写**现状**，不写"计划要做"（计划放 issue 或分支说明里）；设计动机与取舍要写清楚，这是维护者最需要的部分。
- 文档里的每条断言都应能在代码里找到出处，涉及具体数值（间隔、批量、上限）时与代码保持一致。
- 已知限制与坑要如实记录（包括"暂时没做"的部分），避免后来者重复踩坑。
