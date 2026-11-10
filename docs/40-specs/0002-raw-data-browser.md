# 0002. 原始数据只读浏览

> 状态：草案
> 最后更新：2026-11-09
> 关联 ADR：无（数据契约见 [data-model.md](../10-architecture/data-model.md)，页面与接口边界见 [web.md](../10-architecture/web.md) 与 [admin.md](../10-architecture/admin.md)）

## 目标

让登录用户在控制台按渠道、时间与执行状态浏览 crawler 留存的原始载荷和详情，用于观察抓取结果与排障，不修改任何数据。

## 非目标

- 不做编辑、删除、重抓、重放或重新解析；
- 不做全文检索、复杂查询或分析看板；
- 不定义 admin REST 端点、DTO、分页参数与错误码，这些仍【待确认】；
- 不把 raw 中的 HTML / 脚本当页面渲染；富文本渲染策略见 [web.md](../10-architecture/web.md)「待确认事项」。

## 用户场景

- 作为运维人员，我希望按渠道和时间筛选最近入库的原始条目，判断抓取是否正常；
- 作为排障人员，我希望打开一条 source 的 raw 与关联 detail，判断失败出在抓取还是解析；
- 作为分析人员，我希望确认 detail 是否已抓取，以及原始载荷是否完整。

## 契约

| 项 | 内容 |
| --- | --- |
| 涉及聚合 | `data_source` / `data_detail` / `channel` |
| 新增/修改端点 | 【待确认】admin REST；范围见 [admin.md](../10-architecture/admin.md)「REST 管理 API」与 [web.md](../10-architecture/web.md)「页面与路由」 |
| 新增错误码 | 【待确认】未登录行为按 [auth.md](../10-architecture/auth.md)「未登录行为」 |
| 前端路由 | 【待确认】 |

## 验收标准

- [ ] 未登录访问页面返回 `302 /login?returnUrl=...`；访问业务 API 返回 `401 application/json`。
- [ ] 页面只读：不提供创建、编辑、删除、重抓入口；API 不暴露写方法。
- [ ] 列表支持按渠道、时间范围、`exec_status` 与 `data_status` 过滤；分页与排序稳定，具体参数【待确认】。
- [ ] 列表显示 source 的渠道、adapter、record_key 摘要、`data_status`、`exec_status`、创建/更新时间。
- [ ] 详情显示 source raw 与关联 `data_detail` 列表；raw 只按纯文本展示，不执行 HTML / 脚本。
- [ ] detail 显示 `fetch_url`、adapter、`data_status`、`exec_status` 与 raw。
- [ ] 空结果、加载中、请求失败、登录过期都有明确状态，不把错误当空数据。
- [ ] 所有查询经 admin API，不直连数据库；raw 内容不进入前端全局 store。

## 边界与异常

- 空数据：显示空态，不报错。
- 超大 raw：展示策略（截断 / 下载 / 分页）【待确认】。
- 二进制或非 UTF-8 raw：编码与展示方式【待确认】。
- 并发/重复提交：本规格只读，不适用。
- 网络失败：统一错误态，可重试；不得乐观显示不存在的数据。

## 验收结论

未实现。实现完成后补充日期、实际行为与规格的差异、遗留问题。
