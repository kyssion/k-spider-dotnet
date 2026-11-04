# 架构决策记录（ADR）

> **状态**：现状（最后核对：2026-11-04）
> 本目录登记契约与架构决策；已确认决策的正文仍以对应权威文档为准，决策记录只写“决定了什么、为什么、影响哪里”。

## 规则

- 新契约或契约变更：先在“待确认”区登记，向项目所有者确认后再写决策记录并同步权威文档；
- 未确认事项不得写进代码或文档正文当作既定事实，只能以【待确认】形式存在；
- 每份决策必须有状态、日期、关联权威文档；文件名使用 `NNNN-短名.md`；
- 决策与权威文档冲突时，以最新已确认决策为准，并在同一次改动中修正权威文档；
- 模板：[0000-template.md](0000-template.md)。

## 待确认

| 事项 | 位置 | 说明 |
|---|---|---|
| `record_key` 精确格式 | `docs/10-architecture/data-model.md` | 前缀、id、hash 输入、时间窗格式与时区 |
| `channel.schedule` JSON | `docs/10-architecture/admin.md` | 字段、时区、错过轮次语义 |
| `channel.extra` 键表 | `docs/10-architecture/crawler.md` | 键名、类型、默认值、所属适配器 |
| `sys_task.task_detail` / `extra` | `docs/10-architecture/distributed.md` | JSON 键、casing、`dispatch_token` 键名 |
| 节点认证 | `docs/10-architecture/distributed.md` | mTLS / 共享 token / 网络隔离 |
| 数据访问选型 | `AGENTS.md` | linq2db 尚未引入依赖，需确认 |
| admin REST 首期端点/DTO | `docs/10-architecture/admin.md`、`docs/10-architecture/web.md` | web 实现前定稿 |
| process 结构化数据契约 | `docs/10-architecture/process.md` | 分析页面依赖该契约 |
| `data_detail` 失败行语义 | `docs/10-architecture/data-model.md` | 是否允许失败 detail 行 |
| 列表即全文的 detail 行 | `docs/10-architecture/crawler.md` | 是否写 `data_detail`、raw 是否复用 |

## 已确认决策

已确认的决策记录：

- [0001 admin 中央调度](0001-admin-central-scheduling.md)
- [0002 gRPC 传输与端口拆分](0002-grpc-transport.md)
- [0003 文档与技能治理](0003-docs-and-skill-governance.md)

分布式与认证规则正文仍见 [distributed.md](../10-architecture/distributed.md) 与 [auth.md](../10-architecture/auth.md)；业务数据规则见 [data-model.md](../10-architecture/data-model.md)。本目录不重复抄写这些正文。
