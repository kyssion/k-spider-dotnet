# docs — 设计文档

面向维护者与 AI 代理，记录 k-spider 的目标架构与功能全集；平台定位见仓库根 [README.md](../README.md)。

`tech/` 统一存放各服务技术方案；跨服务契约（架构、数据模型、分布式、认证）留在本目录。

## 唯一权威分工

同一主题只允许一处定义；其他文档只链接，不复述定义或结论。

| 主题 | 唯一权威 | 其他位置 |
|---|---|---|
| 平台定位、模块清单与状态、仓库结构、快速开始 | [../README.md](../README.md) | AGENTS / architecture 只链接 |
| 开发原则、代码/测试/配置约定、命令 | [../AGENTS.md](../AGENTS.md) | 其他文档链接 |
| 模块边界、依赖方向、机制索引 | [architecture.md](architecture.md) | —— |
| 业务表语义与规则 | [data-model.md](data-model.md) | DDL 为物理结构权威；注释可引用语义 |
| 业务表物理结构 | [../db/001_business_tables.sql](../db/001_business_tables.sql) | data-model 指向 |
| 分布式机制、状态机、调度 | [distributed.md](distributed.md) | `db/002` 为物理结构权威 |
| 系统设施表物理结构 | [../db/002_sys_tables.sql](../db/002_sys_tables.sql) | distributed 指向 |
| 认证行为、接口、配置 | [auth.md](auth.md) | `db/003` 为物理结构权威 |
| 认证表物理结构 | [../db/003_auth_tables.sql](../db/003_auth_tables.sql) | auth 指向 |
| crawler 模块机制、适配器、反爬、仓储 | [crawler.md](tech/crawler.md) | features 只列能力 |
| process 设计 | [process.md](tech/process.md) | architecture 只链接 |
| admin 服务实现结构、REST 管理 API、前端托管 | [admin.md](tech/admin.md) | 调度与认证分别以 [distributed.md](distributed.md) / [auth.md](auth.md) 为准 |
| web 前端技术选型、目录、页面与交互 | [web.md](tech/web.md) | features 只列能力，接口以 [admin.md](tech/admin.md) 为准 |
| 通用技能目录、同步与跨 agent 使用 | [skills.md](skills.md) | AGENTS / README 只链接 |
| 功能与数据源清单 | [features.md](features.md) | 只写 what/status，不写规则细节 |
| 契约/架构决策与待确认事项 | [decisions/README.md](decisions/README.md) | 权威正文仍看对应文档 |
| 文档模板 | [templates/tech-doc.md](templates/tech-doc.md) | 本文维护要求 |
| 文档索引与维护要求 | 本文 | README 只留入口 |

## 文档索引

| 文档 | 内容 |
|---|---|
| [architecture.md](architecture.md) | 目标架构：模块边界、依赖方向、数据链路、机制索引 |
| [auth.md](auth.md) | 认证与账号方案（已确认） |
| [data-model.md](data-model.md) | 业务表语义与设计规则（物理结构落 `db/001`） |
| [crawler.md](tech/crawler.md) | k-spider-crawler 技术文档（抓取服务） |
| [process.md](tech/process.md) | k-spider-process 技术文档（未开始） |
| [admin.md](tech/admin.md) | k-spider-admin 技术文档（骨架） |
| [web.md](tech/web.md) | k-spider web 技术文档（骨架） |
| [distributed.md](distributed.md) | 分布式架构：admin 中央调度 + 节点执行（gRPC 双向流） |
| [features.md](features.md) | 功能与数据源清单：能力、现状与缺口 |
| [skills.md](skills.md) | 通用 Agent Skills 目录、跨 agent 发现与上游同步 |
| [decisions/README.md](decisions/README.md) | 决策记录规则、待确认清单与已确认决策索引 |
| [templates/tech-doc.md](templates/tech-doc.md) | 服务技术文档模板 |

操作手册（命令、约定、扩展套路）在仓库根 [AGENTS.md](../AGENTS.md)。

## 写作与维护规则

- **唯一权威**：同一主题只在一处定义；其他文档只链接。发现重复时保留权威位置，删除其余定义；
- **状态先写**：除索引/模板外，每份设计文档开头必须有 `> **状态**：<状态>（最后核对：YYYY-MM-DD）`；状态取值：`现状 / 已确认 / 目标设计 / 骨架 / 未开始 / 历史`。“已确认”只表示契约/方案已确认，不等于已实现；实现状态只看根 [README.md](../README.md) 状态表；
- **现状与目标分开**：现状断言必须能在代码里找到出处；目标设计必须写明目标，不得冒充现状；
- **未确认不落地**：未确认的契约、参数、接口用【待确认】标注，并在 [decisions/README.md](decisions/README.md) 的待确认清单登记；确认后写决策记录，再同步权威文档与代码；
- **模板与索引**：新增技术文档用 [templates/tech-doc.md](templates/tech-doc.md)；新增文档必须加入本文索引和唯一权威表；
- **改一处，同步另一处**：DDL 是物理结构唯一权威，文档是语义/规则唯一权威；DDL 注释可引用语义，但不作为规则定义处；代码、DDL、契约、配置变更必须在同一次改动中同步相关文档、状态和核对日期；
- **数值与代码**：目标数值（间隔、批量、上限）先写目标值，实现落地后与代码保持一致；
- **已知限制**：如实记录，包括"暂时没做"的部分；
- **自动检查**：提交前运行 `node scripts/check-docs.mjs`，检查链接、索引覆盖、状态标记和过期路径；
- **归档**：v1 代码、部署单元与旧库导出归档在 `src/k-spider-dotnet-old/`，不作现行依据；v1 测试暂留 `src/k-spider-test/`（门禁过渡）。
