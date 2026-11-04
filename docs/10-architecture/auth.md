# k-spider 认证与账号方案

> 本文为认证与账号行为、接口与配置要求的唯一权威；表结构见 2.1。
> **状态**：已确认（最后核对：2026-11-04）。
> 关联：[system-overview.md](system-overview.md)、[data-model.md](data-model.md)、[distributed.md](distributed.md)。

## 一、落点

认证与账号由 `k-spider-admin` 承载，前端页面在 `web/`，账号表为 `sys_user`；模块职责见 [system-overview.md](system-overview.md)。目标同源部署（admin 托管 `web/dist`），当前 web 托管尚未接入。

## 二、账号

### 2.1 账号表

`sys_user` 表结构、索引与字段注释以 [db/003_auth_tables.sql](../../db/003_auth_tables.sql) 为唯一权威；默认账号要求见 2.3。

### 2.2 密码

- 使用 ASP.NET Core `PasswordHasher`（PBKDF2-HMAC，框架默认参数）；
- 哈希自带随机 salt 与迭代参数，`password_hash` 只存完整摘要；
- 登录成功如返回 `SuccessRehashNeeded`，用当前参数重新哈希并更新；
- 新建与重置密码长度 8–128 位，允许空格与 Unicode；默认账号作为初始化例外（见 2.3）。

### 2.3 默认账号

- 初始化脚本在 `sys_user` 为空时插入 `admin`，密码 `admin`；
- 部署完成后修改默认密码；
- 账号恢复由运维通过受控 SQL 或后续 CLI 重置。

## 三、认证

### 3.1 登录

- `POST /api/auth/login`，请求体 `{ "username": "...", "password": "...", "rememberMe": true }`；
- 服务端校验账号存在、`status='active'`、密码摘要匹配；
- 成功后签发 Cookie；
- 勾选“7 天免登录”（`rememberMe=true`）时写入持久 Cookie，`ExpiresUtc=登录时间+7d`；`false` 时写入浏览器会话 Cookie；
- 登录失败统一返回 `401` 与固定文案“用户名或密码错误”。

### 3.2 会话 Cookie

| 项 | 约定 |
|---|---|
| Cookie 名 | 见第 6 节 `Auth:CookieName` |
| HttpOnly | `true` |
| Secure | 按第 5 节 HTTPS 要求 |
| SameSite | `Strict` |
| Path | `/` |
| Domain | host-only |

Cookie 票据声明：

| 声明 | 用途 |
|---|---|
| `sub` | `sys_user` 主键 |
| `name` | 登录名 |

每次请求由 `OnValidatePrincipal` 读取用户行，账号不存在或 `status != 'active'` 时拒绝并清除 Cookie。禁用账号在下一次请求生效。

Data Protection 密钥按第 6 节配置持久化。

### 3.3 修改密码

`POST /api/auth/change-password` 需要当前密码 + 新密码；成功后更新 `password_hash`，密码规则见 2.2。

### 3.4 登出

`POST /api/auth/logout` 清除 Cookie，返回 `204`；前端布局提供登出入口。

### 3.5 未登录行为

| 请求类型 | 行为 |
|---|---|
| 受保护页面 `GET` | `302 /login?returnUrl=...`；登录成功后按 `returnUrl` 返回 |
| 业务 API | `401 application/json` `{ "error": "unauthorized" }` |
| `/api/auth/me` | `401`，前端清理登录态 |
| `/api/health`、`/api/auth/login`、`/login`、静态资源 | 匿名可访问 |

已登录时 `GET /api/auth/me` 返回当前用户与 antiforgery token。服务端做页面级拦截，前端路由守卫作为第二道。

## 四、账号管理

页面路径 `/settings/users`。所有登录用户同权，可访问本页并执行下表操作；除初始化默认账号（2.3）外，账号通过本页创建。

| 操作 | 接口 | 规则 |
|---|---|---|
| 列表 | `GET /api/users` | 返回 id、username、displayName、status、createTime |
| 新建 | `POST /api/users` | 设置初始密码（规则见 2.2）；用户名约束见 2.1 |
| 修改展示名 | `PATCH /api/users/{id}` | 请求体 `{ "displayName": "..." }`；仅改 `display_name` |
| 禁用/启用 | `PATCH /api/users/{id}` | 请求体 `{ "status": "active" \| "disabled" }`；禁用自己或最后一个 active 账号时拒绝请求 |
| 重置密码 | `POST /api/users/{id}/reset-password` | 设置新密码（规则见 2.2）；作用于其他账号，自己改密走 3.3 |

账号写操作写应用日志：操作者、目标账号、动作、时间。

两个 `PATCH /api/users/{id}` 共用路径，请求体至少包含 `displayName` 或 `status` 之一；同时包含时一并更新。是否拆成独立端点【待确认】。

## 五、安全

- 前端从 3.5 的 `/api/auth/me` 响应获取 antiforgery token，保存在内存；
- 除 `POST /api/auth/login` 外，所有 `POST/PUT/PATCH/DELETE` 请求带 `X-CSRF-TOKEN`，服务端用 ASP.NET Core Antiforgery 校验；
- `POST /api/auth/login` 校验 `Origin` / `Referer` 与当前站点同源，并依赖 3.2 的 SameSite 设置；
- 生产环境强制 HTTPS，反向代理启用 HSTS；
- 响应头补齐 `X-Content-Type-Options: nosniff`、`Referrer-Policy`、`X-Frame-Options` / `frame-ancestors`；
- 开发期 Vite 代理下浏览器 `Origin` 是 `http://localhost:5173`，admin 需允许配置的开发源，或改用同源开发方式；生产严格同源；
- Cookie `Secure` 在 HTTPS 下开启；本地 HTTP 开发需允许关闭（仅限 Development 环境）。

## 六、配置

| 配置键 | 默认值 | 说明 |
|---|---|---|
| `Auth:CookieName` | `kspider.auth` | 会话 Cookie 名 |
| `DataProtection:KeysPath` | 空 | 生产必填，密钥环目录 |

全部支持 `K_SPIDER__` 前缀环境变量覆盖，例如 `K_SPIDER__AUTH__COOKIENAME=<自定义名>`。密钥路径由部署方通过环境变量注入，仓库只保留配置键。

## 七、落地改造

服务端（`k-spider-admin`）：

```
Auth/
  SysUser.cs
  UserRepository.cs
  PasswordService.cs
  AuthEndpoints.cs
  UserEndpoints.cs
  AuthPolicies.cs
  CsrfFilter.cs
```

前端（`web/`）：

| 文件 | 对应章节 |
|---|---|
| `pages/LoginPage` | 3.1、3.5 |
| `pages/UsersPage` | 4 |
| 账号菜单“修改密码” | 3.3 |
| `auth/AuthProvider`、`auth/RequireAuth` | 3.5 |
| `api/auth` | 3.1、3.3、3.4、3.5 |
| `api/client` | 3.5、5 |

### 实施顺序

1. 执行 `db/003_auth_tables.sql`；
2. admin 按第 3 节、第 4 节实现认证与账号管理；
3. web 按 3.1、3.5、第 4 节实现页面与路由守卫；
4. 按第 5 节接入 CSRF、HTTPS 与安全头；
5. 按第 2–5 节补单元、集成与 E2E 测试。

## 八、验收

验收以第 2–5 节为准。
