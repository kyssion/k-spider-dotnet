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
