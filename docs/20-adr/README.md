# 架构决策记录（ADR）

> **状态**：现状（最后核对：2026-11-05）
> 本目录登记契约与架构决策；已确认决策的正文仍以对应权威文档为准，决策记录只写“决定了什么、为什么、影响哪里”。

## 规则

- 新契约或契约变更：先在“待确认”区登记，向项目所有者确认后再写决策记录并同步权威文档；
- 未确认事项不得写进代码或文档正文当作既定事实，只能以【待确认】标记或“待确认事项”章节形式存在；
- 每份决策必须有状态、日期、关联权威文档；文件命名与文档状态见 [docs/README.md](../README.md)；
- 决策与权威文档冲突时，以最新已确认决策为准，并在同一次改动中修正权威文档；
- 模板：[0000-template.md](0000-template.md)。

## 待确认

详细描述只在对应权威文档的待确认章节/条目中维护，本表只做入口索引。

| 事项 | 入口 |
|---|---|
| `record_key` 精确格式 | [data-model.md](../10-architecture/data-model.md) |
| `channel.schedule` JSON | [admin.md](../10-architecture/admin.md) |
| `channel.extra` 键表 | [crawler.md](../10-architecture/crawler.md) |
| crawler 抓取配置键 | [crawler.md](../10-architecture/crawler.md) |
| 反爬策略细节 | [crawler.md](../10-architecture/crawler.md) |
| `sys_task.task_detail` / `extra` | [distributed.md](../10-architecture/distributed.md) |
| proto 契约与字段 | [distributed.md](../10-architecture/distributed.md) |
| 端口号与 TLS 配置 | [distributed.md](../10-architecture/distributed.md) |
| 同渠道互斥持久化 | [distributed.md](../10-architecture/distributed.md) |
| 对账参数默认值 | [distributed.md](../10-architecture/distributed.md) |
| 节点认证 | [distributed.md](../10-architecture/distributed.md) |
| 认证账号 PATCH 端点拆分 | [auth.md](../10-architecture/auth.md) |
| detail 写入快照来源 | [data-model.md](../10-architecture/data-model.md) |
| `data_status` 是否跳过详情回填 | [data-model.md](../10-architecture/data-model.md) |
| 双键水位消费侧 | [crawler.md](../10-architecture/crawler.md) / [process.md](../10-architecture/process.md) |
| 数据访问选型 | [../../AGENTS.md](../../AGENTS.md) |
| admin REST 首期端点/DTO | [admin.md](../10-architecture/admin.md) / [web.md](../10-architecture/web.md) |
| admin 后台页面范围 | [admin.md](../10-architecture/admin.md) |
| web 首期页面范围与交互 | [web.md](../10-architecture/web.md) |
| process 结构化数据契约 | [process.md](../10-architecture/process.md) |
| process 节点接入 | [process.md](../10-architecture/process.md) / [distributed.md](../10-architecture/distributed.md) |
| `data_detail` 失败行语义 | [data-model.md](../10-architecture/data-model.md) |
| 列表即全文的 detail 行 | [crawler.md](../10-architecture/crawler.md) |

## 已确认决策

已确认的决策记录：

- [0001 admin 中央调度](0001-admin-central-scheduling.md)
- [0002 gRPC 传输与端口拆分](0002-grpc-transport.md)
- [0003 文档与技能治理](0003-docs-and-skill-governance.md)

分布式与认证规则正文仍见 [distributed.md](../10-architecture/distributed.md) 与 [auth.md](../10-architecture/auth.md)；业务数据规则见 [data-model.md](../10-architecture/data-model.md)。本目录不重复抄写这些正文。
