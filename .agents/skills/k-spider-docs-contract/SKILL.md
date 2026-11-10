---
name: k-spider-docs-contract
description: >-
  k-spider 的文档与契约变更判别：先改哪份权威文档、何时登记待确认 / ADR、
  DDL 与文档如何同步、文档守卫会拦什么。任务涉及契约、DDL、调度、认证、ADR、
  规格、目录索引或文档结构时使用。条款本身不在技能里，见 docs/ 与 50-ai/。
---

# k-spider 文档与契约变更

> 本技能不复制条款。权威规则见 [docs/README.md](../../../docs/README.md)、
> [20-adr/README.md](../../../docs/20-adr/README.md)、[change-protocol.md](../../../docs/50-ai/change-protocol.md)
> 与 [definition-of-done.md](../../../docs/50-ai/definition-of-done.md)。

## 先定向

1. 读 [AGENTS.md](../../../AGENTS.md) 与 [docs/README.md](../../../docs/README.md)「内容归属」，确定唯一来源。
2. 契约 / DDL / 调度 / 认证：先查 [20-adr/README.md](../../../docs/20-adr/README.md)「待确认」；确认后写 ADR，再改权威文档与代码。
3. 用户可见行为：写 `docs/40-specs/` 规格；结构性变更写 ADR / 待确认，不写规格。
4. 进度只写 [roadmap.md](../../../docs/00-overview/roadmap.md)；ADR 正文不承载进度。

## 常见坑

- 只改 README / roadmap，不同步权威文档与 DDL；
- 把【待确认】项写成现状；
- 修改已确认 ADR 正文，或往 ADR 里加待办；
- 新增文档不登记所属目录 README，或新增事实不查内容归属；
- 状态块、目录索引、ADR 索引、待确认索引只改一处；
- 语义重复但措辞不同，`check-doc-duplication` 抓不到，只能靠评审。

## 自检

- 契约 / 架构待确认在 [20-adr/README.md](../../../docs/20-adr/README.md)；指南 / 工具待确认在
  [30-guides/README.md](../../../docs/30-guides/README.md)「指南待确认索引」。
- ADR 只允许改状态行与索引；旧 ADR 的待办是历史快照，进度见 roadmap。
- 运行 `./scripts/verify.sh`；至少跑四个文档检查与技能清单。
