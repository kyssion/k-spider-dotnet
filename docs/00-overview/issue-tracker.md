# 问题追踪

> 状态：持续更新
> 最后更新：2026-11-08

已发现但尚未处理的问题，以及**可复现步骤、当前影响、处理方向**。

存在的意义：让「我知道这里有问题」不再只存在于某次对话里。条目被修复后**不删除**，
改为在「结论」列写明结果 —— 记录「曾经错在哪、怎么定的」比只留一份待办更有价值。

## 开放中

| # | 问题 | 影响 | 复现/证据 | 处理方向 |
| --- | --- | --- | --- | --- |
| 1 | v1 测试仍是唯一后端门禁 | 新服务没有自己的测试，门禁覆盖与 v2 代码脱节 | `src/k-spider-test/` 引用归档项目；见 [testing.md](../30-guides/testing.md)「测试分层」 | 新服务测试项目落地后替换，v1 测试不长期双轨 |
| 2 | `src/k-spider-grpc/` 尚未创建 | admin / crawler 无法共享 proto 与生成代码 | 见 [distributed.md](../10-architecture/distributed.md)「gRPC 双向流」 | 作为 M2 第一步创建，先定 proto 再写调度 |
| 3 | admin / crawler / web 只有骨架，process 未开始 | 无法端到端跑通「抓取 → 处理 → 展示」 | 根 [README.md](../../README.md) 状态表 | 按 [roadmap.md](roadmap.md) 的 M2–M5 推进 |
| 4 | 前端构建产物尚未接入 admin 托管 | web 只能独立开发服务器运行，生产同源链路未验证 | 见 [admin.md](../10-architecture/admin.md)「前端托管」与 [web.md](../10-architecture/web.md)「配置」 | M5 接入同源部署并补 E2E |
| 5 | 认证、调度、对账只有设计，没有实现 | 无法用测试证明契约成立 | 见 [auth.md](../10-architecture/auth.md)、[distributed.md](../10-architecture/distributed.md) | 实现时逐条对应「测试与守门」章节补测试 |
| 6 | 待确认事项较多且分散在架构文档 | 实现前需要逐项确认，容易漏 | 集中索引见 [20-adr/README.md](../20-adr/README.md)「待确认」 | 实现对应模块前先关闭该模块的待确认项 |
| 7 | `src/k-spider-dotnet-old/` 归档仍在仓库 | 全仓检索、依赖扫描与新手阅读会命中历史代码 | 根 [README.md](../../README.md) 项目结构 | 新测试替换 v1 后评估移出仓库或保持只读归档 |
| 8 | 没有本地 `stop.sh` / `dev.sh` | 后台进程收尾靠手工按端口检查，容易遗漏 | 见 [dev-workflow.md](../30-guides/dev-workflow.md)「后台进程收尾」 | 出现重复手工操作或端口冲突时再补脚本，规则先留在文档 |
| 9 | HTTPS 推送在修改 `.github/workflows/` 时被拒绝 | PAT 缺 `workflow` 权限时，CI 相关提交无法通过 HTTPS 推送 | `remote rejected ... without 'workflow' scope` | 改用 SSH 远端（`git@github.com:kyssion/k-spider-dotnet.git`，本次已通过 SSH 推送），或给 PAT 补 `workflow` 权限 |

## 已处理

| # | 问题 | 结论 |
| --- | --- | --- |
| A | 文档引用漂移只能靠人工审计 | 已加 [check-doc-references.mjs](../../scripts/check-doc-references.mjs)：链接、行内路径、链接文字、章节引用、目录链接与措辞统一由 verify 拦截 |
| B | 同一事实在多处写成正文，副本会漂移 | 已在 [docs/README.md](../README.md)「写作原则」立「一处编写，其余引用」，并加 [check-doc-duplication.mjs](../../scripts/check-doc-duplication.mjs) 拦截精确重复行 |
| C | 目录索引、状态块、ADR 章节、代码块语言只有约定没有守卫 | 已加 [check-doc-structure.mjs](../../scripts/check-doc-structure.mjs)，覆盖状态块/日期、目录索引、ADR 必备章节、架构骨架、标题编号与代码块语言 |
| D | 状态词表多处、索引状态与正文状态可能不一致 | 已统一到 [docs/README.md](../README.md)「文档状态」，并加 [check-doc-status.mjs](../../scripts/check-doc-status.mjs) 从示例行读取词表、校验索引与分组 |
| E | 文档检查脚本单文件过大，解析规则会在新检查项之间漂移 | 已拆为引用/重复/结构/状态四个脚本，共用 `scripts/lib/` 的扫描范围、引用解析与文档模型 |
| F | `30-guides/` 沿用了架构文档的七段骨架，指南出现「数据与接口」「配置」等不自然章节 | 已改为指南自己的结构（环境/命令/端口/步骤/门禁/相关文档）；八段骨架只约束 `10-architecture/` |
| G | 技能清单路径与 schema 在检查脚本和同步脚本里各写一份 | 已抽到 `scripts/lib/skills-manifest.mjs`，统一路径、schema、读取、构造与序列化；检查与同步共用 |
| H | 文档索引与引用可达性会把代码块里的示例链接当成真实登记 | 已在 `scripts/lib/doc-model.mjs` 统一跳过 fenced code；`referencesOf`、`indexRows` 与章节标题校验同时生效 |

## 如何新增条目

1. 先在对应权威文档中确认行为是否符合既有约定 —— 有些「问题」其实是没读文档；
2. 填写可复现的证据（命令 + 输出，或最小复现步骤），不要只写「有问题」；
3. 修复后把条目从「开放中」移到「已处理」，在「结论」列写清最终怎么定的；涉及结构性决策时补 ADR 并在此引用编号。
