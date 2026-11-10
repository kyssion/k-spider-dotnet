# k-spider 认证与账号方案

> 状态：持续更新
> 最后更新：2026-11-10

本文定义认证与账号的行为、接口与配置要求；表结构以
[003_auth_tables.sql](../../db/003_auth_tables.sql) 为唯一权威。相关模块与表结构见本文「相关文档」。

## 定位与职责

认证与账号由 `k-spider-admin` 承载，前端页面在 `web/`，账号表为 `sys_user`；模块职责见 [system-overview.md](system-overview.md)。admin 服务端认证、账号管理与同源静态托管已落地；web 登录/账号页面尚未接入，真实数据库与浏览器链路尚未联调。

## 结构与约定

### 账号表

`sys_user` 表结构、索引与字段注释以 [003_auth_tables.sql](../../db/003_auth_tables.sql) 为唯一权威；默认账号要求见本文「默认账号」。

### 密码

- 使用 ASP.NET Core `PasswordHasher`（PBKDF2-HMAC，框架默认参数）；
- 哈希自带随机 salt 与迭代参数，`password_hash` 只存完整摘要；
- 登录成功如返回 `SuccessRehashNeeded`，用当前参数重新哈希并更新；
- 新建与重置密码长度 8–128 位，允许空格与 Unicode；默认账号作为初始化例外。

### 默认账号

- 初始化脚本在 `sys_user` 为空时插入 `admin`，密码 `admin`；
- 部署完成后修改默认密码；
- 账号恢复由运维通过受控 SQL 或后续 CLI 重置。

### 会话 Cookie 与声明

| 项 | 约定 |
|---|---|
| Cookie 名 | 见本文「配置」中的 `Auth:CookieName` |
| HttpOnly | `true` |
| Secure | 按“安全”中的 HTTPS 要求 |
| SameSite | `Strict` |
| Path | `/` |
| Domain | host-only |

Cookie 票据声明：

| 声明 | 用途 |
|---|---|
| `sub` | `sys_user` 主键 |
| `name` | 登录名 |

每次请求由 `OnValidatePrincipal` 读取用户行，账号不存在或 `status != 'active'` 时拒绝并清除 Cookie。禁用账号在下一次请求生效。Data Protection 密钥按“配置”持久化。

### 安全

- 前端从 `/api/auth/me` 响应获取 antiforgery token，保存在内存；
- 除 `POST /api/auth/login` 外，所有 `POST/PUT/PATCH/DELETE` 请求带 `X-CSRF-TOKEN`，服务端用 ASP.NET Core Antiforgery 校验；
- `POST /api/auth/login` 校验 `Origin` / `Referer` 与当前站点同源，并依赖 SameSite 设置；
- 生产环境强制 HTTPS，反向代理启用 HSTS；
- 响应头补齐 `X-Content-Type-Options: nosniff`、`Referrer-Policy`、`X-Frame-Options` / `frame-ancestors`；
- 开发期 Vite 代理下浏览器 `Origin` 是 `http://localhost:5173`，admin 需允许配置的开发源，或改用同源开发方式；生产严格同源；
- Cookie `Secure` 在 HTTPS 下开启；本地 HTTP 开发需允许关闭（仅限 Development 环境）。

## 核心流程

### 登录

`POST /api/auth/login`，请求体 `{ "username": "...", "password": "...", "rememberMe": true }`；服务端校验账号存在、`status='active'`、密码摘要匹配；成功后签发 Cookie。

勾选“7 天免登录”（`rememberMe=true`）时写入持久 Cookie，`ExpiresUtc = 登录时间 + 7d`；`false` 时写入浏览器会话 Cookie。登录失败统一返回 `401` 与固定文案“用户名或密码错误”。

### 修改密码与登出

- `POST /api/auth/change-password` 需要当前密码 + 新密码；成功后更新 `password_hash`，密码规则见本文「密码」；
- `POST /api/auth/logout` 清除 Cookie，返回 `204`；前端布局提供登出入口。

### 未登录行为

| 请求类型 | 行为 |
|---|---|
| 受保护页面 `GET` | `302 /login?returnUrl=...`；登录成功后按 `returnUrl` 返回 |
| 业务 API | `401 application/json` `{ "error": "unauthorized" }` |
| `/api/auth/me` | `401`，前端清理登录态 |
| `/api/health`、`/api/auth/login`、`/login`、静态资源 | 匿名可访问 |

已登录时 `GET /api/auth/me` 返回当前用户与 antiforgery token。服务端做页面级拦截，前端路由守卫作为第二道。

### 账号管理

页面路径 `/settings/users`。所有登录用户同权，可访问本页并执行下表操作；除初始化默认账号外，账号通过本页创建。

| 操作 | 接口 | 规则 |
|---|---|---|
| 列表 | `GET /api/users` | 返回 id、username、displayName、status、createTime |
| 新建 | `POST /api/users` | 设置初始密码；用户名约束见 `sys_user` DDL |
| 修改展示名 | `PATCH /api/users/{id}` | 请求体 `{ "displayName": "..." }`；仅改 `display_name` |
| 禁用/启用 | `PATCH /api/users/{id}` | 请求体 `{ "status": "active" \| "disabled" }`；禁用自己或最后一个 active 账号时拒绝 |
| 重置密码 | `POST /api/users/{id}/reset-password` | 设置新密码；作用于其他账号，自己改密走“修改密码” |

账号写操作写应用日志：操作者、目标账号、动作、时间。两个 `PATCH /api/users/{id}` 共用路径，请求体至少包含 `displayName` 或 `status` 之一；同时包含时一并更新。是否拆成独立端点【待确认】。

## 数据与接口

- 账号数据：`sys_user`；密码只存哈希；
- 认证接口：`/api/auth/login`、`/api/auth/logout`、`/api/auth/change-password`、`/api/auth/me`；
- 账号管理接口：`/api/users` 与 `/api/users/{id}` 系列。

### 服务端落点

```text
Auth/
  AuthOptions.cs
  AuthRegistration.cs
  PasswordService.cs
  AuthService.cs
  UserService.cs
  AuthEndpoints.cs
  UserEndpoints.cs
  CsrfMiddleware.cs
  SecurityHeadersMiddleware.cs
  CurrentUser.cs

Repository/Postgres/
  PostgresUserRepository.cs
```

### 前端落点（待实现）

| 文件 | 对应章节 |
|---|---|
| `pages/LoginPage` | 登录、未登录行为 |
| `pages/UsersPage` | 账号管理 |
| 账号菜单“修改密码” | 修改密码 |
| `auth/AuthProvider`、`auth/RequireAuth` | 未登录行为与路由守卫 |
| `api/auth` | 登录、修改密码、登出、当前用户 |
| `api/client` | 未登录处理与 CSRF |

具体页面与路由以 [web.md](web.md) 为准。

### 实施状态

1. [003_auth_tables.sql](../../db/003_auth_tables.sql) 由部署时初始化数据库执行；
2. admin 服务端认证、账号管理与 CSRF/安全头已实现；
3. web 登录、账号页面与路由守卫待实现；
4. HTTPS/HSTS 与反向代理按部署配置；
5. 服务端单元/集成测试与前端 E2E 仍待补齐。

## 配置

| 配置键 | 默认值 | 说明 |
|---|---|---|
| `Auth:CookieName` | `kspider.auth` | 会话 Cookie 名 |
| `Auth:RememberMeDays` | `7` | “7 天免登录”持久 Cookie 天数 |
| `Auth:AllowedOrigins` | 空 | 登录 Origin/Referer 白名单；开发期包含 Vite 源 |
| `DataProtection:KeysPath` | 空 | 生产必填，密钥环目录 |

全部支持 `K_SPIDER__` 前缀环境变量覆盖，例如 `K_SPIDER__AUTH__COOKIENAME=<自定义名>`。密钥路径由部署方通过环境变量注入，仓库只保留配置键。

## 测试与守门

- admin 服务端已实现登录/登出/改密/当前用户与账号管理端点，核心依赖通过 `tests/k-spider-admin-tests/` 的 DI 图测试；
- 账号管理的用户名/密码校验、禁用自己、最后一个 active 账号与重置自己密码的边界已有测试；
- 登录成功/失败、Cookie 属性、持久/会话 Cookie 分支、禁用账号立即失效、CSRF、Origin/Referer 与安全头的测试仍待补；
- 前端登录跳转与路由守卫的 E2E 随 web 页面落地；
- 验收以本文“结构与约定”“核心流程”中的规则为准；安全、配置与账号管理边界逐条对照。

## 待确认事项

| 事项 | 说明 |
|---|---|
| PATCH 端点拆分 | `displayName` 与 `status` 是否拆成独立端点 |
| admin REST 首期端点/DTO | 认证相关接口已定；其余管理端点由 admin/web 首期范围决定 |
| 权限模型 | 当前所有登录用户同权；是否需要角色/权限体系 |
| 账号恢复方式 | 受控 SQL 之外的 CLI 是否需要落地 |

## 相关文档

- [system-overview.md](system-overview.md)、[admin.md](admin.md)、[web.md](web.md)：认证的承载与调用方；
- [003_auth_tables.sql](../../db/003_auth_tables.sql)：认证表物理结构；
- [distributed.md](distributed.md)：节点认证与内网信任边界。
