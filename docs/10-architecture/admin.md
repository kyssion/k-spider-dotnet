# k-spider-admin 技术文档

> 状态：草案
> 最后更新：2026-11-08

本文定义 admin 服务的实现落点与模块结构；调度、认证与业务数据语义分别以对应权威文档为准
（见本文「相关文档」）。当前只有最小可运行入口，业务实现待补；模块状态见根
[README.md](../../README.md) 状态表。

## 定位与职责

旁路管理控制服务：中央调度、集群维护、REST 管理 API、认证与账号、web 前端托管。admin 不在数据链路上，不抓取、不加工业务数据；它负责决定“谁在什么时候执行哪个渠道轮”，并把执行结果收敛回任务状态。

当前仓库的 admin 只有 `Program.cs` 的最小入口（`GET /` 返回服务名）；以下结构与流程是目标设计，落地时以本节为落点、以 distributed/auth 为规则。

## 结构与约定

计划中的目录与职责（待实现）：

| 目录 | 职责 |
|---|---|
| `Hosting/` | 启动、配置加载、日志、健康检查、优雅停机 |
| `Scheduler/` | 读 `channel.schedule` 生成任务包、派发互斥、节点选择 |
| `Reconciliation/` | 对账循环、探测、重派、取消与任务表治理 |
| `Grpc/` | `Connect` 双向流、上行/下行消息、写流串行化 |
| `Api/` | REST 管理 API、DTO、分页筛选、错误处理 |
| `Auth/` | 登录、会话、账号管理、CSRF 与安全头 |
| `WebHosting/` | `wwwroot` 静态文件、SPA fallback、缓存策略 |
| `Repository/` | `sys_node` / `sys_task` / `channel` 读写与事务边界 |
| `Common/` | 无业务语义的通用工具 |

边界：调度规则不写在 admin 的端点里，统一放 `Scheduler/` 与 `Reconciliation/`；认证规则不在 admin 重复定义，按 [auth.md](auth.md) 实现；`sys_node` / `sys_task` 的语义与状态机不在 admin 重复定义，按 [distributed.md](distributed.md) 实现。

## 核心流程

### 启动与监听

启动时加载配置与日志，恢复非终态任务的互斥映射；REST 与 gRPC 的监听端点按
[distributed.md](distributed.md)「监听端点」配置。健康检查、优雅停机与 admin 单机约束随实现补齐。

### 调度与对账

`scheduler` 读 `channel.schedule` 生成渠道轮任务包，按节点负载派发；同一渠道同一时刻只派发一个任务。`reconciliation` 周期扫描非终态任务，按“无活动超时 + 探测”判死并重派；重派达上限转 `abandoned`。机制与参数见 [distributed.md](distributed.md)。

### gRPC 节点服务

处理 `Connect` 双向流的生命周期、注册/复活、心跳、任务下发、取消、探测与终态回报；每连接写操作串行化。proto 与生成代码计划放 `src/k-spider-grpc/`（待创建）。

### REST 管理 API

承载 web 的管理与查询接口；端点清单、DTO、分页与筛选、错误码在首期范围确定后补入本文。认证相关接口以 [auth.md](auth.md) 为准，不在此重复。

### 认证与账号

按 [auth.md](auth.md) 接入 Cookie 会话、Data Protection、Antiforgery、登录/账号管理端点与默认账号处理；admin 只负责落点，不重新定义行为。

### 前端托管

`wwwroot` 提供 web 构建产物，SPA fallback 到 `index.html`；构建产物由 [dev-workflow.md](../30-guides/dev-workflow.md) 的 `build-web.sh` 复制，缓存策略与同源部署随实现补齐。

## 数据与接口

- 读取 `channel` / `channel_category` 生成调度任务；读写 `sys_node` / `sys_task` 维护节点与任务状态；
- 业务数据查询按 web 首期需求与 [data-model.md](data-model.md) 的语义设计，不在 admin 另建一套数据定义；
- 事务边界、`KDbException` 与批量写入按 [AGENTS.md](../../AGENTS.md) 与数据访问约定实现；
- REST API 的认证、CSRF 与错误响应以 [auth.md](auth.md) 与首期 API 契约为准；当前尚无独立 API 契约文档，首期端点确定后补。

## 配置

- admin 自身配置的来源与优先级见 [AGENTS.md](../../AGENTS.md)；
- 监听端口（REST / gRPC）与数据库连接由 admin 自身配置；认证配置键见 [auth.md](auth.md)「配置」，
  调度与对账参数见 [distributed.md](distributed.md)「配置」；
- 当前 `appsettings.json` 只有日志与 `AllowedHosts`；业务配置随实现补齐，未定项标【待确认】。

## 测试与守门

- 调度状态机、派发互斥、对账重派、取消检查点、条件更新必须有测试；
- REST API、认证、gRPC 契约测试在实现后补；
- Live 用例与门禁分层见 [testing.md](../30-guides/testing.md)「测试分层」；
- 当前 admin 只有最小入口，尚无业务测试；不能把骨架可运行当作功能已验证。

## 待确认事项

- 调度、对账、gRPC、端口与 TLS 的待确认项以 [distributed.md](distributed.md) 的“待确认事项”为准；
- 认证与账号的待确认项以 [auth.md](auth.md) 的“待确认事项”为准；
- admin REST 首期端点/DTO 与后台页面范围待 web 首期页面确定后设计，见 [web.md](web.md)；
- admin 目录结构与模块拆分在实现前确认，避免先建空目录；
- 部署方式、systemd 托管、日志与回滚方案待补。

## 相关文档

- [system-overview.md](system-overview.md)、[distributed.md](distributed.md)、[auth.md](auth.md)、[web.md](web.md)：admin 的上游规则与下游调用方；
- [dev-workflow.md](../30-guides/dev-workflow.md)：启动命令、端口与前端构建；
- [roadmap.md](../00-overview/roadmap.md)：M2 / M5 里程碑。
