# 术语表

> **状态**：现状（最后核对：2026-11-04）
> 数据实体语义以 [data-model.md](../10-architecture/data-model.md) 为唯一权威；本文只统一业务语言。

| 术语 | 含义 | 对应 |
|---|---|---|
| 源站 / 站点 | 外部数据站点 | `channel_category` 根节点 |
| 渠道 | 一个可拉取的数据供给单元 | `channel` |
| 适配器 | 列表 / 详情的抓取实现目录 | `crawler_adapter` |
| 资源 / 列表条目 | 一条待抓取的资源连接 | `data_source` |
| 详情内容 | 一个资源产出的原始内容 | `data_detail` |
| 数据集 / 管线 | v1 概念；v2 不建独立实体 | 由 `channel` + adapter 表达 |
| 任务包 | admin 派发给节点的一轮执行单元 | `sys_task` |
| 节点 | 执行任务包的 crawler / process 进程 | `sys_node` |
