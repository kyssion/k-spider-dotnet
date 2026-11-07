# 0001 admin 中央调度

> 状态：已确认
> 最后更新：2026-11-04
> 关联：[distributed.md](../10-architecture/distributed.md)
> 影响：admin 调度循环、crawler 节点职责、任务表

## 背景

crawler 需要横向扩缩，同时避免多个节点各自按 `channel.schedule` 起轮次造成重复抓取和同源 QPS 叠加。

## 决策

admin 是唯一调度器：读取 `channel.schedule` 生成渠道轮任务包，按节点负载派发；crawler 只执行收到的任务包，不自行发现渠道、不自行定时。调度现场落 `sys_task` / `sys_node`，admin 重启后靠重连与对账恢复。

## 影响与取舍

- 好处：渠道级派发互斥、任务可观测、节点无本地持久状态；
- 代价：admin 成为控制面单点，需要自动重启与对账兜底；
- 具体状态机、通信协议和重派规则见 [distributed.md](../10-architecture/distributed.md)，本文不重复。
