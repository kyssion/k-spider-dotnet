# 0003 文档与技能治理

> 状态：已确认
> 最后更新：2026-11-04
> 关联：[docs/README.md](../README.md)、[skills.md](../50-ai/skills.md)
> 影响：文档目录、ADR、技能同步与检查脚本

## 背景

项目同时服务人和 AI 代理。文档如果没有唯一权威和状态标记，容易被后续 agent 写成与代码冲突的“目标即现状”；上游 .NET 技能如果按目录硬拷贝，也会在插件结构变化时静默少导或误删自有技能。

## 决策

- 文档按 `00-overview / 10-architecture / 20-adr / 30-guides / 40-specs / 50-ai` 组织；
- 每份设计文档必须有状态和最后核对日期；未确认事项登记到 ADR 索引，不写成既定事实；
- 技能统一放在 `.agents/skills/` 平铺目录，受管上游技能由 `.agents/dotnet-skills/manifest.json` 记录；
- 同步脚本解析上游插件清单，只增删 manifest 登记的目录，不覆盖自有技能；
- 文档引用、状态、索引和过期路径由 `scripts/check-doc-references.mjs` 守住。

## 影响与取舍

- 好处：文档和技能都可被不同 agent/model 复用，漂移可被机器检查；
- 代价：文档变更需要同步状态和索引，技能同步需要 Node.js 与 git；
- 具体写作规则见 [docs/README.md](../README.md)，技能同步规则见 [skills.md](../50-ai/skills.md)。
