# 贡献指南

> 这是给人的上手入口。AI 代理请读 [AGENTS.md](AGENTS.md) —— 红线与阅读路由在那里。
> 本文保持简短：不复制 `docs/` 的内容，只说明去哪里看、哪些事必须先做。

## 快速开始

按 [getting-started.md](docs/30-guides/getting-started.md) 完成环境、数据库与首次验证。
完整命令、端口与脚本行为见 [dev-workflow.md](docs/30-guides/dev-workflow.md)。

## 提交前必须通过

```bash
./scripts/verify.sh
```

它跑哪些环节见 [dev-workflow.md](docs/30-guides/dev-workflow.md)「脚本行为」。
**红灯不提交、更不推送**（红线 12 见 [AGENTS.md](AGENTS.md)）。

`verify.sh` 同时运行 v2 架构/契约守卫与 v1 过渡测试；v2 服务测试仍待 M2/M3，通过不代表 v2 服务功能完成。
服务状态见根 [README.md](README.md) 状态表。v2 服务测试落地前，交付说明必须列出未覆盖项；
[testing.md](docs/30-guides/testing.md)「当前门禁与目标门禁」一节给出判据与 M1.5 目标。

## 动手前：确认需要哪些前置产物

按改动类型决定先写规格、ADR 还是架构文档 —— 分类表、强制顺序、文档同步矩阵只在
[change-protocol.md](docs/50-ai/change-protocol.md) 维护，本文不复制。判定不清时按**更严格**的一类处理。

## 提交、分支与推送

格式与推送要求见 [dev-workflow.md](docs/30-guides/dev-workflow.md)「提交与推送」；只有项目所有者
明确指示时才提交或推送。

## 提交前检查

逐项过 [definition-of-done.md](docs/50-ai/definition-of-done.md)，不要在这里另维护一份清单。

## 代码约定

不在这里重复：[AGENTS.md](AGENTS.md)「代码约定」是唯一来源。按改动位置查：

| 改哪里 | 看哪里 |
| --- | --- |
| 数据模型、写入规则、DDL | [data-model.md](docs/10-architecture/data-model.md) 与 `db/001_business_tables.sql` |
| 调度、任务、节点 | [distributed.md](docs/10-architecture/distributed.md) 与 `db/002_sys_tables.sql` |
| 认证、会话、账号 | [auth.md](docs/10-architecture/auth.md) 与 `db/003_auth_tables.sql` |
| crawler 分层、适配器、反爬 | [crawler.md](docs/10-architecture/crawler.md) |
| admin 服务、REST、前端托管 | [admin.md](docs/10-architecture/admin.md) |
| web 页面、API 客户端、状态管理 | [web.md](docs/10-architecture/web.md) |
| 测试分层与门禁 | [testing.md](docs/30-guides/testing.md) |

新增特性的完整步骤见 [add-a-feature.md](docs/30-guides/add-a-feature.md)；技能按任务加载 1~2 个，
不预读全部，路由见 [AGENTS.md](AGENTS.md)「技能」。

## 文档

文档索引、写作规则与内容归属见 [docs/README.md](docs/README.md)；契约变更与文档更新必须在同一次
改动内完成。

## 安全

发现漏洞请**不要开公开 issue**，流程见 [SECURITY.md](SECURITY.md)。
