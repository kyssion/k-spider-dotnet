# 决策记录

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
| `record_key` 精确格式 | `docs/data-model.md` | 前缀、id、hash 输入、时间窗格式与时区 |
| `channel.schedule` JSON | `docs/tech/admin.md` | 字段、时区、错过轮次语义 |
| `channel.extra` 键表 | `docs/tech/crawler.md` | 键名、类型、默认值、所属适配器 |
| `sys_task.task_detail` / `extra` | `docs/distributed.md` | JSON 键、casing、`dispatch_token` 键名 |
| 节点认证 | `docs/distributed.md` | mTLS / 共享 token / 网络隔离 |
| 数据访问选型 | `AGENTS.md` | linq2db 尚未引入依赖，需确认 |
| admin REST 首期端点/DTO | `docs/tech/admin.md`、`docs/tech/web.md` | web 实现前定稿 |
| process 结构化数据契约 | `docs/tech/process.md` | 分析页面依赖该契约 |
| `data_detail` 失败行语义 | `docs/data-model.md` | 是否允许失败 detail 行 |
| 列表即全文的 detail 行 | `docs/tech/crawler.md` | 是否写 `data_detail`、raw 是否复用 |

## 已确认决策

已确认的分布式与认证规则见 [distributed.md](../distributed.md) 与 [auth.md](../auth.md)；业务数据规则见 [data-model.md](../data-model.md)。本目录暂不重复抄写这些正文。
