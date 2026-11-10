# 0009 ADR 不承载进度：移除模板待办

> 状态：已确认
> 最后更新：2026-11-09
> 决策人：项目所有者（执行指令）
> 关联：[README.md](README.md)、[roadmap.md](../00-overview/roadmap.md)、[issue-tracker.md](../00-overview/issue-tracker.md)
> 影响：ADR 模板与规则、0005–0008 的「待办」、roadmap 的进度职责、testing / DoD 的状态表述

## 背景

ADR README 规定「ADR 只增不改；仅允许更新状态行与索引」，但模板又包含 `## 待办` 复选框。实际使用中，
0005–0008 的「待办」被持续勾选和补写，导致三个问题：

- ADR 正文在确认后被修改，违反「只增不改」，决策历史不再可信；
- 同一项进度同时出现在 ADR、roadmap、testing 的「当前/目标」表和 issue-tracker 中；
- 读者无法判断「ADR 的待办」与「roadmap 的勾选」哪一个才是当前状态。

## 备选项

| 选项 | 优点 | 代价 |
| --- | --- | --- |
| A. 保留 ADR 待办，放宽「只增不改」 | 一处记录决策与进度 | 决策记录变成活文档，历史快照失效；仍需与 roadmap 同步 |
| B. ADR 只记录决策，进度归 roadmap / issue-tracker（选择） | 决策历史稳定；进度只有一个来源 | 需要迁移现有「待办」；读者要跳到 roadmap 看进展 |
| C. 保留模板但冻结待办，不再勾选 | 改动最小 | 复选框会长期停在过期状态，误导读者 |

## 决策

选择 B。ADR 正文只记录背景、备选项、决策与后果；**不包含可更新的待办或进度**。
进度以 [roadmap.md](../00-overview/roadmap.md) 为准，具体问题以
[issue-tracker.md](../00-overview/issue-tracker.md) 为准，门禁判据以
[testing.md](../30-guides/testing.md) 为准。0005–0008 的「待办」保留为旧模板的历史快照，不再更新。

## 后果

- 正面：ADR 的「只增不改」重新成立；进度只有一个来源；决策与执行状态不再互相污染。
- 负面（需要接受的代价）：0005–0008 中已有待办与 roadmap 重复；读者需要习惯从 roadmap 看进度；
  删除模板待办后，ADR 不再能直接列出实施动作。
- 需要同步修改的位置：ADR README 的「规则」与「模板」、[roadmap.md](../00-overview/roadmap.md)、
  [testing.md](../30-guides/testing.md) 的进度表述、[definition-of-done.md](../50-ai/definition-of-done.md) 的状态说明。
- 触发重新评估的条件：出现必须与决策正文绑定的实施约束（例如迁移/回滚步骤），且放在 roadmap 会丢失上下文。
