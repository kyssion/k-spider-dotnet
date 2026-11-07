# 新增一个特性

> 状态：持续更新
> 最后更新：2026-11-08
> 权威范围：从一个需求到可交付改动的分步做法。变更分类与文档同步矩阵以
> [change-protocol.md](../50-ai/change-protocol.md) 为准。

把「先契约、后实现、同一次提交同步文档」的顺序写成可执行步骤，避免先写代码再补契约，
或把目标设计写成现状。

## 先判断改动类型

按 [change-protocol.md](../50-ai/change-protocol.md) 的分类判断：功能、结构、契约、修复、重构、
文档/配置。判断不清时按更严格的一类处理。所需产物也以该协议为准：功能写规格，结构/契约先登记
待确认项，修复补回归测试，重构保持行为不变。

## 权威文档先行

契约变更必须按「文档 → 后端 → 前端 → 验证」的顺序；禁止先改前端再改后端。各主题的唯一权威见
[docs/README.md](../README.md)「内容归属」。

## 步骤

1. **定向**：读 [AGENTS.md](../../AGENTS.md) 与 [docs/README.md](../README.md)，再读相关架构文档；
   必要时只加载 1~2 个技能；
2. **分类**：确定是功能、结构、契约、修复、重构还是文档改动；
3. **补产物**：功能写规格；结构/契约写 ADR 或待确认项；用户可见行为写验收标准；
4. **确认**：不可逆或影响公共契约的决策，先向项目所有者确认，不自行拍板；
5. **实现**：契约 → 数据访问 → 服务 → 接口/前端；保持小步，不顺手重构无关代码；
6. **同步文档**：更新权威文档、状态、目录索引和待确认清单；新增架构文档按
   [tech-doc-template.md](tech-doc-template.md) 的骨架写；
7. **补测试**：按 [testing.md](testing.md) 的分层补测试；修复必须带回归用例；
8. **验证**：跑 `./scripts/verify.sh`，记录实际输出；
9. **交付**：按 [collaboration-model.md](../50-ai/collaboration-model.md) 的四段式汇报，
   列出未覆盖项与后续。

## 按主题查契约

- 涉及数据模型或 DDL：先改 [data-model.md](../10-architecture/data-model.md) 与
  [001_business_tables.sql](../../db/001_business_tables.sql) 的契约，再改代码；
- 涉及调度/任务：以 [distributed.md](../10-architecture/distributed.md) 为准；
- 涉及认证/账号：以 [auth.md](../10-architecture/auth.md) 为准；
- 涉及 REST/gRPC 契约：先改对应架构文档，再改实现与前端类型。

## 配置与凭据

- 新增配置键必须走配置/Options，不硬编码；
- 配置键与凭据注入方式以 [AGENTS.md](../../AGENTS.md) 的配置约定为准；
- 新增配置键同步所属技术文档的「配置」章节。

## 测试与门禁

- 功能变更至少有验收标准对应的测试；
- 修复必须有回归测试，且不得删除或弱化现有测试；
- 文档改动必须跑 `./scripts/verify.sh`（或至少四个文档检查脚本）；
- 交付前跑 `./scripts/verify.sh`，并确认没有遗留后台进程。

## 待确认事项

- 规格模板与验收结论的细化程度；
- 新服务测试项目落地后，本文是否需要增加「测试项目接入」步骤；
- 前端页面的组件、状态管理与 API 客户端约定待 [web.md](../10-architecture/web.md) 确认。

## 相关文档

- [change-protocol.md](../50-ai/change-protocol.md)：变更分类、产物与同步矩阵；
- [testing.md](testing.md)：测试分层与验证闭环；
- [tech-doc-template.md](tech-doc-template.md)：架构文档骨架；
- [definition-of-done.md](../50-ai/definition-of-done.md)：交付前自检。
