# 变更协议

> 状态：已确认
> 最后更新：2026-11-08
本文范围：变更分类、所需产物、强制顺序、文档同步矩阵、验证与回滚。红线与代码约定见 [AGENTS.md](../../AGENTS.md)。

## 为什么需要它

规定“一次改动应该走什么路径”，让影响范围可预测、事后可复盘。判断不清时按更严格的一类处理；多写一篇 ADR 的成本低于一次错误重构。

## 变更分类与所需产物

| 类型 | 触发条件 | 必需产物 |
|---|---|---|
| A. 功能变更 | 用户可见行为变化 | `docs/40-specs/` 规格 + 代码 + 测试 + 契约文档 |
| B. 结构变更 | 分层、依赖、目录、可观测性变化 | `docs/20-adr/` 决策记录 + 架构文档更新 |
| C. 契约变更 | 路由、字段、错误码、状态码、DDL、调度、认证语义变化 | 待确认项/ADR + 对应权威文档 + 前后端同步 |
| D. 修复 | 行为与规格或契约不符 | 回归测试 + 规格/契约对照说明 |
| E. 重构 | 外部行为完全不变 | 无文档；提交信息注明“行为不变”，测试必须全绿 |
| F. 文档/配置 | 只改文档或非行为性配置 | 无；仍要跑文档检查 |

## 标准流程

1. **分类**：按上表确定改动类型；
2. **补产物**：功能写规格；结构/契约先在 [20-adr/README.md](../20-adr/README.md) 登记待确认项，确认后写 ADR；
3. **确认**：涉及不可逆决策、公共接口、数据模型、DDL、调度或认证时，停止等待项目所有者确认；
4. **实现**：从内到外，契约 → 数据访问 → 服务 → 接口/前端；每层补测试；
5. **同步文档**：契约、模型、配置、状态、目录索引、待确认清单在同一次改动内完成；
6. **验证**：按 [testing.md](../30-guides/testing.md) 的分层跑测试，再跑 `./scripts/verify.sh`；
7. **收尾**：确认没有遗留后台进程与临时文件；
8. **交付**：按 [collaboration-model.md](collaboration-model.md) 的格式汇报，附带实际命令输出。

## 契约变更的强制顺序

契约是前后端的共同财产，顺序错乱会导致一方短暂失配。必须按以下顺序：

1. 修改对应权威文档（例如 [data-model.md](../10-architecture/data-model.md)、[distributed.md](../10-architecture/distributed.md)、[auth.md](../10-architecture/auth.md)）；
2. 修改 DDL / 后端 DTO / 端点 / 错误码，并补后端测试；
3. 修改前端类型、API 客户端与视图；
4. 跑 `./scripts/verify.sh`。

禁止先改前端再改后端，也禁止“两边同时改然后祈祷一致”。

## 破坏性变更

以下任一项必须先写 ADR 并确认：

- 删除或重命名已发布的字段、端点或错误码；
- 改变字段类型或语义；
- 收紧取值约束；
- 改变状态机、调度语义或认证行为；
- 修改 DDL 中已有列的含义。

处理方式按 ADR 记录的迁移与回滚方案执行；未确认前不落代码。

## 文档同步矩阵

| 改动内容 | 必须同步更新 |
|---|---|
| 新增/修改功能范围 | `docs/40-specs/` 规格；完成后补验收结论 |
| 新增/修改数据模型或 DDL | [data-model.md](../10-architecture/data-model.md) 与 `db/001_business_tables.sql` |
| 新增/修改系统设施表 | [distributed.md](../10-architecture/distributed.md) 与 `db/002_sys_tables.sql` |
| 新增/修改认证行为或表 | [auth.md](../10-architecture/auth.md) 与 `db/003_auth_tables.sql` |
| 新增/修改调度、任务、节点 | [distributed.md](../10-architecture/distributed.md) |
| 新增/修改 crawler 分层、适配器、反爬 | [crawler.md](../10-architecture/crawler.md) |
| 新增/修改 admin 实现或 REST API | [admin.md](../10-architecture/admin.md)；契约细节按主题指向 distributed/auth |
| 新增/修改 web 页面或前端约定 | [web.md](../10-architecture/web.md) |
| 新增/修改定位、目标或非目标 | [vision.md](../00-overview/vision.md) |
| 新增/修改里程碑或暂不考虑事项 | [roadmap.md](../00-overview/roadmap.md) |
| 新发现或关闭已知问题 | [issue-tracker.md](../00-overview/issue-tracker.md) |
| 新增/修改功能或数据源清单 | [features.md](../00-overview/features.md) |
| 新增/修改术语 | [glossary.md](../00-overview/glossary.md) |
| 贡献流程或安全策略变化 | [CONTRIBUTING.md](../../CONTRIBUTING.md) / [SECURITY.md](../../SECURITY.md) |
| 新增/修改命令、端口、脚本 | [dev-workflow.md](../30-guides/dev-workflow.md) |
| 新增/修改测试分层或门禁 | [testing.md](../30-guides/testing.md) |
| 新增/删除任何文档 | 所属目录 `README.md` 与 [docs/README.md](../README.md) 的目录索引；新增目录时两边都要登记 |
| 新增/修改待确认项 | [20-adr/README.md](../20-adr/README.md) 与对应权威文档的“待确认事项” |
| 贡献流程、角色或交付格式变化 | [collaboration-model.md](collaboration-model.md) 与 [definition-of-done.md](definition-of-done.md) |
| 技能目录或同步方式变化 | [skills.md](skills.md) |

记忆口诀：**改了对外契约，就改 `10-architecture/`；改了未来要做的，就改 [roadmap.md](../00-overview/roadmap.md) /
[issue-tracker.md](../00-overview/issue-tracker.md)；改了别人怎么参与，就改 [CONTRIBUTING.md](../../CONTRIBUTING.md) /
[SECURITY.md](../../SECURITY.md)；改了命令与门禁，就改 [dev-workflow.md](../30-guides/dev-workflow.md) /
[testing.md](../30-guides/testing.md)。**

## 验证与回滚

- 验证命令与门禁边界见 [testing.md](../30-guides/testing.md)；交付前必须给出实际命令与输出；
- 文档检查失败按输出逐条修正，不绕过、不压警告；
- 回滚以 ADR 记录的方案为准：代码回滚、文档回滚与 DDL 回滚必须在同一次操作中完成；DDL 基线幂等，但不提供自动向下迁移，涉及数据删除的变更必须先确认备份与恢复方案；
- 回滚后重新跑 `./scripts/verify.sh` 并更新文档状态。

## 变更记录

变更历史由 Git 提交承载，提交信息说明本次改了什么、为什么；重大决策的权衡过程由 ADR 承载。
只有提交信息无法还原当时否掉了哪些选项，只有 ADR 无法定位具体落点；两者互补，缺一不可。
提交格式见 [dev-workflow.md](../30-guides/dev-workflow.md)「提交与推送」，ADR 写法见
[20-adr/README.md](../20-adr/README.md)。

## 与红线的关系

红线是禁令集合，本协议是流程；两者冲突时以 [AGENTS.md](../../AGENTS.md) 的红线为准。未在本文列出的通用技术知识不复制，按需查对应权威文档。
