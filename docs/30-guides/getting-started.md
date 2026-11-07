# 首次跑通

> 状态：持续更新
> 最后更新：2026-11-08
> 权威范围：环境准备、数据库初始化、启动与首次验证；日常命令见 [dev-workflow.md](dev-workflow.md)。

## 定位与职责

让新加入的人在最短时间内把仓库跑起来，并能判断“跑起来了”与“业务已实现”不是一回事。平台定位与模块状态以根 [README.md](../../README.md) 为唯一来源。

## 结构与约定

### 环境要求

- .NET SDK：版本由 [global.json](../../global.json) 锁定，当前为 `10.0.100`，`rollForward` 为 `latestFeature`；
- PostgreSQL 15+；
- Node.js 20+ 与 pnpm（仅 web 前端）；
- 可选：Playwright 浏览器（跑前端 E2E 时需要）。

### 前置产物

- 根目录的 `k-spider-dotnet.sln`；
- `db/` 下的三个幂等 DDL：`001_business_tables.sql`、`002_sys_tables.sql`、`003_auth_tables.sql`；
- `web/pnpm-lock.yaml`。

## 核心流程

### 构建后端

```bash
dotnet build k-spider-dotnet.sln
```

### 初始化数据库

创建数据库后按顺序执行 DDL；三个脚本都是幂等的，可重复执行。数据库已存在时跳过 `createdb`：

```bash
createdb k_spider
psql -d k_spider -f db/001_business_tables.sql
psql -d k_spider -f db/002_sys_tables.sql
psql -d k_spider -f db/003_auth_tables.sql
```

物理结构以 DDL 为唯一权威；业务语义、系统设施表与认证表分别见 [data-model.md](../10-architecture/data-model.md)、[distributed.md](../10-architecture/distributed.md)、[auth.md](../10-architecture/auth.md)。

### 启动服务

```bash
dotnet run --project src/k-spider-admin
dotnet run --project src/k-spider-crawler
cd web && pnpm install && pnpm dev
```

开发期 admin 监听 `http://localhost:5094`（见 [launchSettings.json](../../src/k-spider-admin/Properties/launchSettings.json)）；web 开发服务器监听 `http://localhost:5173`，`/api` 代理到 5094（见 [vite.config.ts](../../web/vite.config.ts)）。crawler 当前是 worker 骨架，不监听 HTTP。

当前 admin、crawler、web 都是骨架；能启动不等于业务已实现。模块状态只看根 [README.md](../../README.md) 状态表。

### 首次验证

```bash
./scripts/verify.sh
```

验证环节、Live 用例与门禁边界见 [testing.md](testing.md)；脚本逐项行为见 [dev-workflow.md](dev-workflow.md)。

## 数据与接口

- 数据库是服务间的数据契约：crawler 写原始载荷，process 读原始载荷并写结构化数据，admin 读写系统设施表；
- web 只通过 admin 的同源 `/api` 访问数据，不直连数据库；
- 首次初始化只执行 DDL，不导入业务数据；默认账号等种子数据以 [auth.md](../10-architecture/auth.md) 为准。

## 配置

配置优先级与凭据注入规则以 [AGENTS.md](../../AGENTS.md) 为唯一来源：`appsettings.json → appsettings.{环境}.json → K_SPIDER__ 前缀环境变量 → 代码默认值`。仓库不提交真实凭据。

## 测试与守门

- `./scripts/verify.sh` 是交付前的唯一入口，包含文档四检查、技能清单、前端构建/E2E、后端构建/格式/测试；
- `--skip-tests` 只用于本地冒烟（跳过 E2E 与后端测试），输出会显式标注；交付前必须跑完整校验；
- 文档检查失败时按输出逐条修正，不要绕过。

## 待确认事项

- 生产部署的端口、TLS 与反向代理细节见 [distributed.md](../10-architecture/distributed.md) 与 [admin.md](../10-architecture/admin.md) 的待确认事项；
- 数据库连接串与账号由部署方注入，键名以各服务 `appsettings` 与 [AGENTS.md](../../AGENTS.md) 为准。
