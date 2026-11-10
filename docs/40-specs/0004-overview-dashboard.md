# 0004. 总览

> 状态：实现中
> 最后更新：2026-11-10
> 关联 ADR：无（页面设计见 [web.md](../10-architecture/web.md)，管理 API 见 [admin.md](../10-architecture/admin.md)）

## 目标

让登录用户在控制台首页看到渠道、任务与节点的当前健康度，以及最近失败任务，便于快速判断是否需要处理。

## 非目标

- 不做历史趋势、同比/环比、导出或分析看板；
- 不做渠道、任务、节点的写操作；
- 不定义新的业务表或调度语义；
- 不把 `raw` 或执行详情放进总览响应。

## 用户场景

- 作为运维人员，我希望一眼看到多少渠道启用、多少任务卡在非终态；
- 作为排障人员，我希望看到最近失败任务及失败原因，快速进入任务详情；
- 作为值班人员，我希望知道节点在线数量，判断是否是集群问题。

## 契约

| 项 | 内容 |
| --- | --- |
| 涉及聚合 | `channel`、`sys_task`、`sys_node` |
| 新增/修改端点 | `GET /api/overview`；登录用户可访问，只读 |
| 新增错误码 | 无新增；未登录 `401 unauthorized`，数据库异常 `500 database_error` |
| 前端路由 | `/` |

响应 `OverviewResponse`：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `generatedAt` | string | UTC ISO 8601，响应生成时刻 |
| `channels` | object | `total` / `active` / `paused` / `retired` |
| `tasks` | object | `total` / `byStatus`（八个任务状态计数） |
| `nodes` | object | `total` / `online` / `offline` |
| `recentFailures` | array | 最近失败任务，最多 10 条，按 `updateTime` 倒序 |

`recentFailures[]` 字段：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `taskId` | number | 任务主键 |
| `channelId` | number | 渠道主键，来自 `task_detail.channel_id` |
| `nodeId` | string | 执行节点 uuid |
| `status` | string | `push_failed` / `run_failed` / `abandoned` |
| `failCount` | number | `extra.fail_count` |
| `lastError` | string? | `extra.last_error`，超长截断 |
| `updatedAt` | string | UTC ISO 8601 |

## 验收标准

- [ ] 未登录访问 `GET /api/overview` 返回 `401 application/json`。
- [ ] 空库返回 200，渠道/任务/节点计数均为 0，`recentFailures` 为空数组。
- [ ] `channels` 各状态计数之和等于渠道总数；`nodes` 在线/离线计数之和等于节点总数。
- [ ] `tasks.byStatus` 覆盖八个状态，计数与 `sys_task` 当前快照一致。
- [ ] `recentFailures` 最多 10 条，按 `updateTime` 倒序，状态只包含失败/放弃类。
- [ ] 响应只读，不提供任何写方法。
- [ ] 前端总览页展示加载、空、错误与登录过期状态，不把失败当空数据。

## 边界与异常

- 空数据：所有计数返回 0，`recentFailures` 返回空数组。
- 大表统计：计数使用聚合查询；若性能下降，再评估物化/缓存，不在首期引入。
- 并发更新：计数是同一请求内的近似快照，不要求跨表事务一致。
- 超长错误：`lastError` 按约定截断，避免响应过大。
- 网络失败：前端统一错误态并可重试，不乐观展示不存在的计数。

## 验收结论

admin 端点、查询服务与 web 总览页已实现；空态/错误态与更多统计口径测试随页面完善补齐。
