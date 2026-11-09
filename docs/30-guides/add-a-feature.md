# 新增一个特性

> 状态：持续更新
> 最后更新：2026-11-09
本文范围：从一个需求到可交付改动的分步做法。变更分类与文档同步矩阵以
[change-protocol.md](../50-ai/change-protocol.md) 为准。

把「先契约、后实现、同一次提交同步文档」的顺序写成可执行步骤，避免先写代码再补契约，
或把目标设计写成现状。

## 先判断改动类型

按 [change-protocol.md](../50-ai/change-protocol.md) 的分类判断：功能、结构、契约、修复、重构、
文档/配置。判断不清时按更严格的一类处理。所需产物也以该协议为准：功能写规格，结构/契约先登记
待确认项，修复补回归测试，重构保持行为不变。

## 权威文档先行

契约变更必须按「文档 → 后端 → 前端 → 验证」的顺序；禁止先改前端再改后端。各主题的唯一权威见
[docs/README.md](../README.md)「内容归属」。

## 改动落点速查

下表只回答「这类改动先看哪份契约、代码大概落在哪」，文档同步矩阵以
[change-protocol.md](../50-ai/change-protocol.md)「文档同步矩阵」为准。

| 改动 | 先改的契约 | 代码落点 | 验证重点 |
| --- | --- | --- | --- |
| 业务表 / DDL | [data-model.md](../10-architecture/data-model.md) 与 [001_business_tables.sql](../../db/001_business_tables.sql) | repository、服务、写入路径 | DDL 幂等、write_mode、双键水位、update_time 刷新 |
| 系统设施表 / 调度 | [distributed.md](../10-architecture/distributed.md) 与 [002_sys_tables.sql](../../db/002_sys_tables.sql) | admin 的 Scheduler / Reconciliation / Grpc | 状态机、派发 token、重派、取消检查点 |
| 认证 / 账号 | [auth.md](../10-architecture/auth.md) 与 [003_auth_tables.sql](../../db/003_auth_tables.sql) | admin 的 Auth、web 登录与路由守卫 | 会话、Cookie 属性、CSRF、禁用账号生效 |
| crawler 源接入 | [crawler.md](../10-architecture/crawler.md) 与 `channel.extra` 键表 | adapter / parser / repository | 夹具回归、翻页停止、失败重试边界 |
| admin REST / 托管 | [admin.md](../10-architecture/admin.md) | Api / WebHosting | 端点契约、错误码、SPA fallback |
| web 页面 / 交互 | [web.md](../10-architecture/web.md) | `web/src/api` → `pages` / `components` / `stores` | 主路径、错误态、路由守卫的 E2E |
| 用户可见功能范围 | [40-specs/README.md](../40-specs/README.md) 与对应规格 | 按上述落点拆小步 | 验收标准逐条有测试或标注未覆盖 |

### 按落点的最小检查清单

**crawler 新源 / 适配器**

- 先读 [crawler.md](../10-architecture/crawler.md) 与 [data-model.md](../10-architecture/data-model.md)；
- 适配器编码登记到 `crawler_adapter`，渠道行配置 source / detail adapter、write_mode、retention、schedule、extra；
- 源差异只落在 adapter + parser，请求统一走 HttpGateway；
- 补离线夹具回归、write_mode / 双键水位 / update_time 刷新测试；
- 同步 `channel.extra` 键表与待确认事项。

**admin REST / 认证**

- 先读 [admin.md](../10-architecture/admin.md) 与 [auth.md](../10-architecture/auth.md)；
- 端点、DTO、错误码、CSRF 与未登录行为先写进权威文档；
- 补契约测试：状态码、错误码、Cookie / CSRF、禁用账号；
- 前端 API 客户端与页面在同一次改动内对齐。

**web 页面**

- 先读 [web.md](../10-architecture/web.md) 与 [auth.md](../10-architecture/auth.md)；
- 请求收敛到 `web/src/api/`，页面不直接 `fetch`；
- 主路径、加载态、空态、错误态、路由守卫都要有 E2E 或组件测试；
- 新增页面同步 web.md 的页面与路由。

**DDL / 数据模型**

- 先改 [data-model.md](../10-architecture/data-model.md) 与对应 DDL；
- 物理结构只写 DDL，语义规则只写文档；
- 补 repository 的写入模式、水位、一致性与异常边界测试；
- 破坏性变更先在 [20-adr/README.md](../20-adr/README.md) 登记待确认。

## 步骤

1. **定向**：读 [AGENTS.md](../../AGENTS.md) 与 [docs/README.md](../README.md)，再读相关架构文档；
   必要时只加载 1~2 个技能；
2. **分类**：确定是功能、结构、契约、修复、重构还是文档改动；
3. **补产物**：功能写规格；结构/契约写 ADR 或待确认项；用户可见行为写验收标准；
4. **确认**：不可逆或影响公共契约的决策，先向项目所有者确认，不自行拍板；
5. **实现**：契约 → 数据访问 → 服务 → 接口/前端；保持小步，不顺手重构无关代码；
6. **同步文档**：更新权威文档、状态、目录索引和待确认清单；新增架构文档按
   [10-architecture/README.md](../10-architecture/README.md)「技术文档骨架」写；
7. **补测试**：按 [testing.md](testing.md) 的分层补测试；修复必须带回归用例；
8. **验证**：跑 `./scripts/verify.sh`，记录实际输出；
9. **交付**：按 [collaboration-model.md](../50-ai/collaboration-model.md) 的四段式汇报，
   列出未覆盖项与后续。

## 按主题查契约

- 涉及数据模型或 DDL：先改 [data-model.md](../10-architecture/data-model.md) 与
  [001_business_tables.sql](../../db/001_business_tables.sql) 的契约，再改代码；
- 涉及调度/任务：以 [distributed.md](../10-architecture/distributed.md) 为准；
- 涉及认证/账号：以 [auth.md](../10-architecture/auth.md) 为准；
- 涉及 REST/gRPC 契约：先改对应架构文档，再改实现与前端类型。

## 配置与凭据

- 新增配置键必须走配置/Options，不硬编码；
- 配置键与凭据注入方式以 [AGENTS.md](../../AGENTS.md) 的配置约定为准；
- 新增配置键同步所属技术文档的「配置」章节。

## 测试与门禁

- 功能变更至少有验收标准对应的测试；
- 修复必须有回归测试，且不得删除或弱化现有测试；
- 文档改动必须跑 `./scripts/verify.sh`（或至少四个文档检查脚本）；
- 交付前跑 `./scripts/verify.sh`，并确认没有遗留后台进程。

## 常见失败点

| 失败点 | 避免方式 |
| --- | --- |
| 先写前端，再回头补契约 | 按本文「权威文档先行」的顺序，先改权威文档与后端契约 |
| 只改 DDL / 只改文档，另一侧漂移 | 物理结构以 DDL 为准，语义以 [data-model.md](../10-architecture/data-model.md) 为准；同一次改动同步 |
| 新增配置键但没进文档 | 配置键同步所属技术文档「配置」章节，凭据只存引用 |
| 新端点没有登记契约与错误码 | 先登记权威文档与错误码，再写端点测试 |
| 页面直接 `fetch` 或复制服务端数据到全局 store | 请求收敛到 `web/src/api`，服务端状态交给 TanStack Query |
| 忘记目录索引、状态块或待确认清单 | 交付前对照 [definition-of-done.md](../50-ai/definition-of-done.md) 逐项检查 |
| 用跳过测试或放宽断言让 verify 变绿 | 禁止；覆盖不足时在交付说明中标注未覆盖项 |

## 待确认事项

- 规格模板与验收结论的细化程度；
- 新服务测试项目落地后，本文是否需要增加「测试项目接入」步骤；
- 前端页面的组件、状态管理与 API 客户端约定待 [web.md](../10-architecture/web.md) 确认。

## 相关文档

- [change-protocol.md](../50-ai/change-protocol.md)：变更分类、产物与同步矩阵；
- [testing.md](testing.md)：测试分层与验证闭环；
- [10-architecture/README.md](../10-architecture/README.md)：架构文档骨架与索引；
- [definition-of-done.md](../50-ai/definition-of-done.md)：交付前自检。
