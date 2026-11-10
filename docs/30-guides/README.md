# 指南索引

> 状态：持续更新
> 最后更新：2026-11-09

本目录回答「怎么跑、怎么测、怎么改」。命令、端口与脚本行为以 [dev-workflow.md](dev-workflow.md)
为唯一来源；技术文档骨架见 [10-architecture/README.md](../10-architecture/README.md)「技术文档骨架」，
写作规则见 [docs/README.md](../README.md)；变更流程与角色分工归 `50-ai/`。

| 文档 | 回答的问题 |
| --- | --- |
| [getting-started.md](getting-started.md) | 环境准备、数据库初始化、首次启动与验证 |
| [dev-workflow.md](dev-workflow.md) | 常用命令、端口与同步点、脚本行为与选型、进程收尾、提交与推送 |
| [testing.md](testing.md) | 测试分层、当前与目标门禁、v2 测试接入、断言与隔离、Live 用例、夹具 |
| [add-a-feature.md](add-a-feature.md) | 从一个需求到可交付改动的分步做法 |

首次跑通后，日常改动按 [add-a-feature.md](add-a-feature.md) 执行；交付前按
[definition-of-done.md](../50-ai/definition-of-done.md) 自检。

## 指南待确认索引

本目录的待确认项只做入口索引，细节以原文档为准；契约 / 架构 / 跨模块设计类待确认见
[20-adr/README.md](../20-adr/README.md)「待确认」。

| 事项 | 入口 |
| --- | --- |
| crawler/process 服务测试项目命名、目录与夹具库 | [testing.md](testing.md)「待确认事项」 |
| 组件测试覆盖率目标与页面 E2E 边界 | [testing.md](testing.md)「待确认事项」 |
| Live 用例触发方式与凭据 | [testing.md](testing.md)「待确认事项」 |
| v1 测试替换时间与迁移范围 | [testing.md](testing.md)「待确认事项」/ [issue-tracker.md](../00-overview/issue-tracker.md) #1 |
| CI 缓存与耗时调优 | [0007-ci-parallel-jobs.md](../20-adr/0007-ci-parallel-jobs.md) / [testing.md](testing.md)「待确认事项」 |
| crawler 本地启动参数与端口 | [dev-workflow.md](dev-workflow.md)「待确认事项」 |
| 生产部署托管、日志与回滚 | [dev-workflow.md](dev-workflow.md)「待确认事项」/ [roadmap.md](../00-overview/roadmap.md)「M6」 |
| 生产端口、TLS 与反向代理 | [getting-started.md](getting-started.md)「待确认事项」/ [distributed.md](../10-architecture/distributed.md)「待确认事项」 |
| 数据库连接串与账号键名 | [getting-started.md](getting-started.md)「待确认事项」/ [AGENTS.md](../../AGENTS.md) |
| 规格模板与验收结论细化程度 | [add-a-feature.md](add-a-feature.md)「待确认事项」/ [40-specs/README.md](../40-specs/README.md) |
| 新服务测试项目接入步骤 | [add-a-feature.md](add-a-feature.md)「待确认事项」 |
| 首期页面交互细节 | [web.md](../10-architecture/web.md)「待确认事项」 |
