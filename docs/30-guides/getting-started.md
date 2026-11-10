# 首次跑通

> 状态：持续更新
> 最后更新：2026-11-10
本文范围：环境准备、数据库初始化、启动与首次验证；日常命令见 [dev-workflow.md](dev-workflow.md)。

让新加入的人在最短时间内把仓库跑起来，并能判断「跑起来了」与「业务已实现」不是一回事。
平台定位见 [vision.md](../00-overview/vision.md)；模块状态以根 [README.md](../../README.md) 状态表为准。

## 环境要求

- .NET SDK：版本由 [global.json](../../global.json) 锁定，当前为 `10.0.100`，`rollForward` 为 `latestFeature`；
- PostgreSQL 15+；
- Node.js 20+ 与 pnpm（仅 web 前端）；
- Playwright 浏览器：完整 `./scripts/verify.sh` 需要；首次运行
  `pnpm --dir web exec playwright install chromium`。

## 前置产物

- 根目录的 `k-spider-dotnet.sln`；
- `db/` 下的三个幂等 DDL：`001_business_tables.sql`、`002_sys_tables.sql`、`003_auth_tables.sql`；
- `web/pnpm-lock.yaml`。

## 构建后端

```bash
dotnet build k-spider-dotnet.sln
```

## 初始化数据库

创建数据库后按顺序执行 DDL；三个脚本都是幂等的，可重复执行。数据库已存在时跳过 `createdb`：

```bash
createdb k_spider
psql -d k_spider -f db/001_business_tables.sql
psql -d k_spider -f db/002_sys_tables.sql
psql -d k_spider -f db/003_auth_tables.sql
```

物理结构以 DDL 为唯一权威；业务语义、系统设施表与认证表分别见
[data-model.md](../10-architecture/data-model.md)、[distributed.md](../10-architecture/distributed.md)、
[auth.md](../10-architecture/auth.md)。

## 启动服务

admin 启动前必须提供数据库连接串（示例值按本地环境替换）：

```bash
export K_SPIDER__DATABASE__CONNECTIONSTRING='Host=localhost;Database=k_spider;Username=<user>;Password=<password>'
dotnet run --project src/k-spider-admin
dotnet run --project src/k-spider-crawler
cd web && pnpm install && pnpm dev
```

开发期 admin REST 监听 `http://localhost:5094`、gRPC 监听 `http://localhost:5095`
（见 [appsettings.json](../../src/k-spider-admin/appsettings.json) 的 `Admin:Endpoints`）；web 开发服务器监听
`http://localhost:5173`，`/api` 代理到 5094（见 [vite.config.ts](../../web/vite.config.ts)）。
crawler 当前是 worker 骨架，不监听 HTTP。

模块状态以根 [README.md](../../README.md) 状态表为准；能启动不等于业务已实现。

## 首次验证

```bash
./scripts/verify.sh
```

当前 verify 同时运行 v2 架构/契约守卫、admin 核心测试与 v1 过渡测试；守卫只验证解决方案布局、DDL 与门禁配置，
admin 核心测试验证状态机/调度/对账，v1 测试验证归档代码与夹具回归，都不证明 crawler/process 已完成。
crawler/process 服务测试随 M3/M4 落地后替换（[issue-tracker.md](../00-overview/issue-tracker.md) #1）。
验证环节、Live 用例与门禁边界见
[testing.md](testing.md)；脚本逐项行为见 [dev-workflow.md](dev-workflow.md)。

## 配置与凭据

配置优先级与凭据注入规则见 [AGENTS.md](../../AGENTS.md)。数据库连接串与账号由部署方注入，仓库不提交真实凭据。

## 数据契约

- 服务间数据契约以 [data-model.md](../10-architecture/data-model.md) 与
  [distributed.md](../10-architecture/distributed.md) 为准；
- web 的数据入口以 [web.md](../10-architecture/web.md)「数据与接口」为准；
- 首次初始化只执行 DDL，不导入业务数据；默认账号等种子数据以 [auth.md](../10-architecture/auth.md) 为准。

## 待确认事项

- 生产部署的端口、TLS 与反向代理细节见 [distributed.md](../10-architecture/distributed.md) 与
  [admin.md](../10-architecture/admin.md) 的待确认事项；
- 数据库连接串与账号的键名以各服务 `appsettings` 与 [AGENTS.md](../../AGENTS.md) 为准。

## 相关文档

- [dev-workflow.md](dev-workflow.md)：日常命令、端口、脚本行为与进程收尾；
- [testing.md](testing.md)：测试分层、夹具与门禁边界；
- [system-overview.md](../10-architecture/system-overview.md)：模块边界与依赖方向；
- [CONTRIBUTING.md](../../CONTRIBUTING.md)：贡献流程与提交前检查。
