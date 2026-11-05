# 文档索引

> **状态**：现状（最后核对：2026-11-05）
> 本目录是项目唯一权威知识源；代码回答“怎么做”，文档回答“为什么这样做”和“约定是什么”。平台定位与模块状态以根 [README.md](../README.md) 为唯一来源。

## 目录职责

| 目录 | 回答的问题 | 何时更新 |
|---|---|---|
| `00-overview/` | 功能范围与术语 | 能力、范围或术语变化时 |
| `10-architecture/` | 系统怎么分层、数据/调度/认证/服务怎么设计 | 结构或契约变化时，必须与代码同一次提交 |
| `20-adr/` | 某个契约或架构决策为什么这么定、还有哪些未确认项 | 做难以回退的决策时先登记，确认后写决策记录 |
| `30-guides/` | 技术文档怎么写 | 写作方式变化时 |
| `40-specs/` | 某个功能做到什么程度算完成 | 开始做功能前写，完成后补验收结论 |
| `50-ai/` | 技能如何组织与同步 | 技能体系变化时 |

## 唯一权威分工

同一主题只允许一处定义；其他文档只链接，不复述定义或结论。

| 主题 | 唯一权威 | 其他位置 |
|---|---|---|
| 平台定位、模块状态、仓库结构、快速开始与命令 | [../README.md](../README.md) | AGENTS / architecture 只链接 |
| 开发原则、红线、工作流、代码约定、技能路由 | [../AGENTS.md](../AGENTS.md) | 其他文档链接 |
| 模块边界、依赖方向、机制索引 | [10-architecture/system-overview.md](10-architecture/system-overview.md) | —— |
| 业务表语义与规则 | [10-architecture/data-model.md](10-architecture/data-model.md) | DDL 为物理结构权威；注释可引用语义 |
| 业务表物理结构 | [../db/001_business_tables.sql](../db/001_business_tables.sql) | data-model 指向 |
| 分布式机制、状态机、调度 | [10-architecture/distributed.md](10-architecture/distributed.md) | `db/002_sys_tables.sql` 为物理结构权威 |
| 系统设施表物理结构 | [../db/002_sys_tables.sql](../db/002_sys_tables.sql) | distributed 指向 |
| 认证行为、接口、配置 | [10-architecture/auth.md](10-architecture/auth.md) | `db/003_auth_tables.sql` 为物理结构权威 |
| 认证表物理结构 | [../db/003_auth_tables.sql](../db/003_auth_tables.sql) | auth 指向 |
| crawler 模块机制、适配器、反爬、仓储 | [10-architecture/crawler.md](10-architecture/crawler.md) | features 只列能力 |
| process 设计 | [10-architecture/process.md](10-architecture/process.md) | architecture 只链接 |
| admin 服务实现 | [10-architecture/admin.md](10-architecture/admin.md) | 调度与认证分别以 distributed / auth 为准 |
| web 前端 | [10-architecture/web.md](10-architecture/web.md) | features 只列能力，接口以 admin 为准 |
| 功能与数据源清单 | [00-overview/features.md](00-overview/features.md) | 只写 what/status，不写规则细节 |
| 术语 | [00-overview/glossary.md](00-overview/glossary.md) | 其他文档引用 |
| 通用技能目录、同步与跨 agent 使用 | [50-ai/skills.md](50-ai/skills.md) | AGENTS / README 只链接 |
| 契约/架构决策与待确认事项 | [20-adr/README.md](20-adr/README.md) | 权威正文仍看对应文档 |
| 技术文档模板 | [30-guides/tech-doc-template.md](30-guides/tech-doc-template.md) | 本文维护要求 |
| 功能规格规则 | [40-specs/README.md](40-specs/README.md) | 规格正文各自登记 |
| 文档索引与维护要求 | 本文 | README 只留入口 |

## 非 docs 协作文档

| 位置 | 作用 |
|---|---|
| `README.md` | 平台定位、模块状态、快速开始 |
| `AGENTS.md` | AI 协作入口：工作流、红线、代码约定、技能路由 |
| `.agents/skills/` | 任务触发的领域知识；上游技能归属见 `.agents/dotnet-skills/manifest.json` |
| `scripts/check-doc-references.mjs` | 文档引用、状态、索引检查 |

## 文件命名

- 目录前缀 `00-` `10-` … 固定阅读顺序；
- ADR 与规格统一四位序号：`0001-短名.md`，ADR 只增不改；
- 文件名使用英文小写短横线，正文用中文。

## 文档状态

除索引/模板外，每份设计文档开头必须有：

```markdown
> **状态**：现状 / 已确认 / 目标设计 / 骨架 / 未开始 / 历史（最后核对：YYYY-MM-DD）
```

“已确认”只表示契约/方案已确认，不等于已实现；实现状态只看根 [README.md](../README.md) 状态表。

## 写作原则

1. **现状与目标分开**：现状必须能在代码中找到出处，目标设计不得冒充现状；
2. **未确认不落地**：按 [20-adr/README.md](20-adr/README.md) 的规则登记待确认事项，不把猜测写成事实；
3. **先结论后论证**：开头三行说清楚读者该记住什么；
4. **写约束不写教程**：只记录“必须怎样 / 禁止怎样”，通用知识不复制；
5. **失效即删除**：代码推翻文档时，同一次提交修正，不留矛盾；
6. **和代码同提交**：代码、DDL、契约、配置、脚本变更必须同步相关文档、状态和核对日期；
7. **自动检查**：提交前运行 `node scripts/check-doc-references.mjs`。

## 阅读路径

- 新加入的开发者：根 `README.md` → `10-architecture/system-overview.md`；
- 接到实现需求：`40-specs/README.md` → 相关架构文档 → 必要时加载 `.agents/skills/`；
- AI 代理：根 `AGENTS.md` → 本文 → 与任务相关的架构文档 / 1~2 个技能。

## 归档

v1 代码、部署单元与旧库导出归档在 `src/k-spider-dotnet-old/`，不作现行依据；v1 测试暂留 `src/k-spider-test/`（门禁过渡）。
