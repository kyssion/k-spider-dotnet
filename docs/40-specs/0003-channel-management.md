# 0003. 渠道管理

> 状态：草案
> 最后更新：2026-11-09
> 关联 ADR：无（渠道语义见 [data-model.md](../10-architecture/data-model.md)，调度见 [distributed.md](../10-architecture/distributed.md)）

## 目标

让登录用户在控制台查看渠道分类、渠道与适配器配置，并通过 admin 调度接口暂停或恢复渠道。

## 非目标

- 不做适配器编码、parser 或抓取逻辑的在线编辑；
- 不做 `channel.extra`、`channel.schedule`、`write_mode`、`retention_days` 的在线修改；
- 不做任务手动触发、取消或重派；这些属于任务控制页面；
- 不定义 admin REST 端点、DTO、错误码与前端路由，这些仍【待确认】。

## 用户场景

- 作为运维人员，我希望按站点/栏目查看渠道清单与适配器，确认当前启用的数据源；
- 作为运维人员，我希望暂停一个异常渠道，避免继续抓取，并在修复后恢复；
- 作为排障人员，我希望看到渠道状态、保留期与调度配置，判断任务为何没有产生。

## 契约

| 项 | 内容 |
| --- | --- |
| 涉及聚合 | `channel_category` / `channel` / `sys_task` |
| 新增/修改端点 | 【待确认】admin REST；暂停/恢复经 admin 调度接口受理，范围见 [features.md](../00-overview/features.md)「控制台功能」与 [distributed.md](../10-architecture/distributed.md)「派发」 |
| 新增错误码 | 【待确认】 |
| 前端路由 | 【待确认】 |

## 验收标准

- [ ] 未登录时页面重定向到 `/login?returnUrl=...`；业务 API 返回 `401 application/json`（按 [auth.md](../10-architecture/auth.md)「未登录行为」）。
- [ ] 列表按分类树展示站点 → 模块 → 栏目；空分类显示空态。
- [ ] 渠道行显示 source_adapter、detail_adapter、write_mode、retention_days、schedule 摘要与 status。
- [ ] 支持按分类与状态过滤；分页与排序稳定，具体参数【待确认】。
- [ ] 暂停需二次确认；成功后渠道状态为 `paused`，不再派发新任务；在跑任务的处理按调度契约【待确认】。
- [ ] 恢复成功后渠道状态为 `active`，后续轮次可以重新派发。
- [ ] 暂停/恢复属于写操作，必须写应用日志（操作者、渠道、动作、时间）并受 CSRF 保护。
- [ ] 首期不提供 adapter 编码、write_mode、retention、schedule、extra 的在线编辑入口。
- [ ] 空列表、加载中、请求失败、无权限/登录过期都有明确状态。

## 边界与异常

- 空分类/空渠道：显示空态，不报错。
- 重复暂停/恢复：幂等处理，不产生重复任务或状态抖动。
- 并发修改：以调度接口的最终状态为准；冲突时给出明确错误。
- 网络失败：不得乐观更新渠道状态；以服务端返回为准。
- 渠道已退役或不存在：拒绝操作并给出明确错误码【待确认】。

## 验收结论

尚未实现；实现后补充验收日期、实际行为差异与遗留问题。
