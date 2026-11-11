# k-spider-admin 技术文档

> 状态：持续更新
> 最后更新：2026-11-11

本文定义 admin 服务的实现结构、调度/对账落点、REST/gRPC/认证与前端托管；调度语义、认证行为与业务数据语义分别以
[distributed.md](distributed.md)、[auth.md](auth.md)、[data-model.md](data-model.md) 为准。模块状态以根
[README.md](../../README.md) 状态表为准。

## 定位与职责

旁路管理控制服务：中央调度、集群维护、REST 管理 API、认证与账号、web 前端托管。admin 不在数据链路上，不抓取、
不加工业务数据；它负责决定“谁在什么时候执行哪个渠道轮”，并把执行结果收敛回任务状态。

当前已落地模块化单体：调度、对账、gRPC 节点服务、REST、认证与静态托管均在同一 ASP.NET Core 项目内，按目录与接口
约束边界（[0010-admin-modular-monolith.md](../20-adr/0010-admin-modular-monolith.md)）。

## 结构与约定

```text
k-spider-admin/
├── Hosting/          组合根：DI、配置校验、Kestrel 双端点、管道与健康检查
├── Scheduler/        调度循环、schedule 解析、同渠道互斥、节点选择、渠道命令
├── Reconciliation/   过期任务扫描、探测、重派、放弃、节点判死、清理
├── Tasks/            任务状态机、派发 token、admin 侧生命周期、节点/探测抽象
├── Grpc/             Connect 双向流、节点连接注册表、下行派发/取消/探测
├── Api/              REST 契约、查询服务、管理端点、异常中间件
├── Auth/             Cookie 会话、CSRF、密码服务、登录/账号管理端点
├── WebHosting/       静态文件与 SPA fallback
├── Repository/       linq2db/PostgreSQL 数据访问、模型与 KDbException
└── Common/           JSONB 契约序列化、周期循环基类、日志事件
```

依赖方向：

- `Hosting` 依赖所有模块；`Common` 不依赖业务模块。
- `Scheduler`、`Reconciliation`、`Grpc` 只依赖 `Tasks/Abstractions` 的节点/探测接口，不互相直接引用。
- API 只调用 `Tasks`、`Scheduler` 与查询服务；所有数据库访问收敛在 `Repository`。
- 后台循环与仓储均按无状态单例注册；每次数据库操作创建独立连接，不持有跨轮次状态。
- 任务状态迁移只允许经过 `Tasks/AdminTaskService`；端点、对账与 gRPC 不直接拼状态更新。

节点连接注册表是**在线判定事实源**，`sys_node` 是镜像；`sys_task` 是任务状态事实源。节点直写 `running` /
`success` / `run_failed`，admin 只写推送系状态、取消、重派与放弃，并在收到 `task_result` 时校验 token、释放
同渠道互斥（[distributed.md](distributed.md)「任务模型」「防竞态写入」）。

## 核心流程

### 启动与监听

1. 加载配置并校验 Options；数据库连接串、端口、认证与对账参数均来自配置。
2. 调度循环首次运行前从 `sys_task` 恢复最近运行时间与同渠道互斥映射。
3. REST 与 gRPC 使用分离的 Kestrel 端点：REST 为 HTTP/1.1，gRPC 为 HTTP/2；默认开发地址分别为
   `127.0.0.1:5094` 与 `127.0.0.1:5095`，端口与绑定地址走配置。

### 调度

`SchedulerLoop` 周期读取 active 渠道，解析 `channel.schedule`（当前实现只支持 `enabled` + `interval_seconds`，
未知键失败），按最近运行时间判断到期；选在线 crawler 节点后创建 `sys_task`（`pushing` + 新 token）并下发
`task_dispatch`。下发成功置 `pushed`，失败置 `push_failed`；同渠道互斥由内存 guard 承担，启动时从非终态任务恢复。

### 对账与节点判死

`ReconciliationLoop` 扫描非终态且 `update_time` 超时的任务；`pushed` / `running` 先探测，探测失败才按
`MaxFailCount` 重派或转 `abandoned`。`NodeHealthMonitor` 按心跳超时把节点写为 offline 墓碑并重派其名下任务；
`TaskCleanupLoop` 分批删除终态任务与过期 offline 节点，非终态行永不清理。

### gRPC 节点通道

`Connect` 首条消息必须是 `register`；校验 `node_type`（crawler/process）后注册连接并 `UpsertOnline` `sys_node`，
返回 `register_ack`。心跳只更新内存快照；`task_result` 只用于校验与唤醒，admin 不替节点写终态；`probe_reply`
按 `request_id` 唤醒探测等待。下行写入按连接串行化并设发送超时。

### REST 管理 API 与认证

REST 端点覆盖认证/账号、渠道、任务、节点与健康检查；总览与原始数据浏览的端点/DTO 已确认，等待实现。
认证使用 Cookie 会话、`PasswordHasher`、Antiforgery；除 `/api/auth/login` 外的写请求统一校验 `X-CSRF-TOKEN`；
登录校验 Origin/Referer。具体行为以 [auth.md](auth.md) 为准。

### 前端托管

`wwwroot` 提供 web 构建产物，带 hash 的静态资源长缓存；SPA fallback 只接管非 `/api`、非 gRPC 路径。
构建产物由 [dev-workflow.md](../30-guides/dev-workflow.md) 的 `build-web.sh` 复制。

## 数据与接口

- 读取 `channel` / `channel_category` 生成调度任务；读写 `sys_node` / `sys_task` 维护节点与任务状态；
- 账号读写 `sys_user`；认证行为以 [auth.md](auth.md) 为准；
- 业务数据查询按 web 首期需求与 [data-model.md](data-model.md) 的语义设计，不在 admin 另建一套数据定义；
- `task_detail` / `extra` 当前使用 snake_case 约定键：`channel_id`、`round_type`、`batch_limit`、`dispatch_token`、
  `fail_count`、`last_error`；完整 JSON 契约仍以 [distributed.md](distributed.md)「待确认事项」为准；
- 数据访问选型与边界见 [0011-admin-data-access-linq2db.md](../20-adr/0011-admin-data-access-linq2db.md)。

### REST 端点状态

| 端点组 | 状态 | 说明 |
| --- | --- | --- |
| `/api/auth/*`、`/api/users` | 已实现 | 登录、登出、改密、当前用户与账号管理 |
| `/api/channels`、`/api/channel-categories` | 已实现 | 渠道查询、暂停/恢复 |
| `/api/tasks`、`/api/nodes` | 已实现 | 任务查询/取消、节点镜像查询 |
| `/api/health` | 已实现 | 数据库健康检查；匿名可访问 |
| `/api/overview` | 已实现 | 渠道/任务/节点统计与最近失败；DTO 见 [0004-overview-dashboard.md](../40-specs/0004-overview-dashboard.md) |
| `/api/raw-data/*` | 已实现 | 只读分页；列表不含 raw，详情返回 base64；DTO 见 [0002-raw-data-browser.md](../40-specs/0002-raw-data-browser.md) |

## 配置

| 配置键 | 默认值 | 说明 |
| --- | --- | --- |
| `Database:ConnectionString` | 空 | PostgreSQL 连接串，部署方注入 |
| `Admin:Endpoints:BindAddress` | `127.0.0.1` | REST/gRPC 绑定地址；生产按部署调整 |
| `Admin:Endpoints:RestPort` | `5094` | REST HTTP/1.1 端口 |
| `Admin:Endpoints:GrpcPort` | `5095` | gRPC HTTP/2 端口 |
| `Admin:Scheduler:TickSeconds` | `5` | 调度循环周期 |
| `Admin:Scheduler:DefaultBatchLimit` | `1000` | 任务包默认批量上限 |
| `Admin:Reconciliation:*` | 见 `appsettings.json` | 对账、探测、重派、心跳与清理参数 |
| `Admin:Grpc:SendTimeoutSeconds` | `5` | 下行写入超时 |
| `Admin:Grpc:ProbeTimeoutSeconds` | `5` | 单次探测超时 |

认证、Cookie 与 Data Protection 配置见 [auth.md](auth.md)「配置」。全部支持 `K_SPIDER__` 前缀环境变量覆盖；
优先级见 [AGENTS.md](../../AGENTS.md)。

## 测试与守门

- admin 核心测试在 `tests/k-spider-admin-tests/`：状态机、schedule 解析、同渠道互斥、任务生命周期、对账重派/
  放弃、节点选择、账号管理边界、总览/原始数据查询映射与 DI 图解析；
- v2 架构/契约守卫在 `tests/k-spider-guards/`；gRPC 消息 round-trip 在 `tests/k-spider-grpc-tests/`；
- 真实 PostgreSQL 的 repository 读写用例在 `tests/k-spider-admin-tests/Live/`，带 `TestCategory=Live`，
  需注入 `K_SPIDER_LIVE_DB` 手动执行，不进入默认门禁；
- 测试分层与门禁边界见 [testing.md](../30-guides/testing.md)。

## 待确认事项

- proto 字段、错误码与流控仍按 [distributed.md](distributed.md)「待确认事项」执行；
- `task_detail` / `extra` 的完整 JSON 契约与 casing 仍需随 proto 定稿；
- `channel.schedule` 的 cron、时区与错过轮次语义；当前只实现 interval；
- 同渠道互斥持久化：继续解析 `task_detail` JSONB，还是把 `channel_id` 提升为独立列并加索引；
- 节点认证、端口与 TLS 配置；
- 部署方式、systemd 托管、日志与回滚方案。

## 相关文档

- [system-overview.md](system-overview.md)、[distributed.md](distributed.md)、[auth.md](auth.md)、[web.md](web.md)：
  admin 的上游规则与下游调用方；
- [data-model.md](data-model.md)：业务表语义；
- [0002-raw-data-browser.md](../40-specs/0002-raw-data-browser.md)、
  [0004-overview-dashboard.md](../40-specs/0004-overview-dashboard.md)：原始数据与总览端点/DTO；
- [0010-admin-modular-monolith.md](../20-adr/0010-admin-modular-monolith.md)、
  [0011-admin-data-access-linq2db.md](../20-adr/0011-admin-data-access-linq2db.md)：admin 结构与数据访问决策；
- [dev-workflow.md](../30-guides/dev-workflow.md)：启动命令、端口与前端构建；
- [roadmap.md](../00-overview/roadmap.md)：M2 / M5 里程碑。
