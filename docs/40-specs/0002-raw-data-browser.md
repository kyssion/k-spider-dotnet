# 0002. 原始数据只读浏览

> 状态：实现中
> 最后更新：2026-11-10
> 关联 ADR：无（数据契约见 [data-model.md](../10-architecture/data-model.md)，页面与接口边界见 [web.md](../10-architecture/web.md) 与 [admin.md](../10-architecture/admin.md)）

## 目标

让登录用户在控制台按渠道、时间与执行状态浏览 crawler 留存的原始载荷和详情，用于观察抓取结果与排障，不修改任何数据。

## 非目标

- 不做编辑、删除、重抓、重放或重新解析；
- 不做全文检索、复杂查询或分析看板；
- 不把 raw 中的 HTML / 脚本当页面渲染；
- 不返回完整 raw 到列表，避免一次响应过大。

## 用户场景

- 作为运维人员，我希望按渠道和时间筛选最近入库的原始条目，判断抓取是否正常；
- 作为排障人员，我希望打开一条 source 的 raw 与关联 detail，判断失败出在抓取还是解析；
- 作为分析人员，我希望确认 detail 是否已抓取，以及原始载荷是否完整。

## 契约

| 项 | 内容 |
| --- | --- |
| 涉及聚合 | `data_source` / `data_detail` / `channel` |
| 新增/修改端点 | 本文「端点与 DTO」；登录用户只读访问 |
| 新增错误码 | 未登录 `401 unauthorized`；参数错误 `400 validation`；不存在 `404 not_found` |
| 前端路由 | `/raw-data`、`/raw-data/sources/:id` |

### 端点与 DTO

| 端点 | 说明 |
| --- | --- |
| `GET /api/raw-data/sources` | source 分页列表；支持 `channelId`、`from`、`to`、`execStatus`、`dataStatus`、`page`、`pageSize` |
| `GET /api/raw-data/sources/{id}` | source 详情；包含 `rawBase64` 与 `rawSize` |
| `GET /api/raw-data/sources/{id}/details` | 指定 source 下的 detail 分页列表；`page`、`pageSize` |
| `GET /api/raw-data/details/{id}` | detail 详情；包含 `rawBase64` 与 `rawSize` |

通用约定：

- `from` / `to` 为 UTC ISO 8601；`page` 从 1 开始；`pageSize` 1–100，默认 20；
- 列表按 `createTime desc, id desc` 稳定排序；
- 列表不返回 raw，只返回 `rawSize`；详情返回 `rawBase64`，前端按 UTF-8 文本展示或下载；
- 所有响应均为只读；不暴露 `POST/PUT/PATCH/DELETE`。

`RawDataSourceListItem`：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `id` | number | source 主键 |
| `channelId` | number | 渠道主键 |
| `channelName` | string | 渠道名（联表） |
| `adapter` | string | 列表适配器编码快照 |
| `recordKey` | string | 资源唯一键 |
| `dataStatus` | string | `normal` / `filtered` / `dead` / `paywalled` |
| `execStatus` | string | `pending` / `running` / `failed` / `success` |
| `rawSize` | number | 原始载荷字节数 |
| `createTime` / `updateTime` | string | UTC ISO 8601 |

`RawDataSourceDetail` 在列表字段基础上增加：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `rawBase64` | string | 原始载荷 Base64 |
| `rawEncoding` | string | 固定 `base64` |

`RawDataDetailListItem`：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `id` | number | detail 主键 |
| `sourceId` | number | 来源 source |
| `fetchUrl` | string | 实际请求地址快照 |
| `adapter` | string | 详情适配器编码快照 |
| `recordKey` | string | 内容唯一键 |
| `dataStatus` | string | 数据事实 |
| `execStatus` | string | 执行进度 |
| `rawSize` | number | detail 原始载荷字节数 |
| `createTime` / `updateTime` | string | detail 创建/更新时间（UTC ISO 8601） |

`RawDataDetailDetail` 在列表字段基础上增加 `rawBase64` 与 `rawEncoding`。

## 验收标准

- [ ] 未登录访问页面返回 `302 /login?returnUrl=...`；访问业务 API 返回 `401 application/json`。
- [ ] 页面只读：不提供创建、编辑、删除、重抓入口；API 不暴露写方法。
- [ ] source 列表支持按渠道、时间范围、`exec_status` 与 `data_status` 过滤；分页稳定，`pageSize` 超过 100 被拒绝或钳制。
- [ ] 列表显示渠道、adapter、record_key 摘要、`data_status`、`exec_status`、raw 大小与创建/更新时间。
- [ ] source 详情显示 raw 与关联 detail 列表；raw 只按纯文本展示，不执行 HTML / 脚本。
- [ ] detail 显示 `fetch_url`、adapter、`data_status`、`exec_status` 与 raw。
- [ ] 空结果、加载中、请求失败、登录过期都有明确状态，不把错误当空数据。
- [ ] 所有查询经 admin API，不直连数据库；raw 内容不进入前端全局 store。

## 边界与异常

- 空数据：显示空态，不报错。
- 超大 raw：详情返回 base64；前端默认截断展示并提供下载，避免一次性渲染超大文本。
- 二进制或非 UTF-8 raw：按 UTF-8 解码失败时显示十六进制/下载入口，不静默丢弃。
- 并发/重复提交：本规格只读，不适用。
- 网络失败：统一错误态，可重试；不得乐观显示不存在的数据。

## 验收结论

admin 端只读端点与查询服务已实现；web 原始数据页面待补。实现完成后补充验收日期、实际行为差异与遗留问题。
