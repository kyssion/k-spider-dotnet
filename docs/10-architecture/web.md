# k-spider web 技术文档

> 状态：持续更新
> 最后更新：2026-11-11

本文定义 web 前端的技术选型、目录结构、页面路由、API 对接、UI 设计系统与测试边界；当前已落地核心控制台页面与
组件测试，原始数据页面待补。模块状态以根 [README.md](../../README.md) 状态表为准；认证接口、admin API 与
数据语义见本文「相关文档」。

## 定位与职责

顶层展示与操作界面：通过同源 admin REST API 获取数据与执行操作；不直连数据库，不承载业务规则。
认证与权限由 admin 承载，前端负责登录态展示、路由守卫、CSRF token 携带与展示时区格式化。

前端定位是**内部运维控制台**，不是营销站点：优先保证信息密度、表格稳定性、错误可定位与首屏速度。

## 结构与约定

### 技术选型

已确定：

- React 19 + TypeScript + Vite 7；
- Tailwind CSS 4 + CSS variables 设计令牌；
- React Router 7；
- TanStack Query 5（服务端状态）；
- Zustand 5（仅客户端 UI 状态）；
- Recharts 3（图表）；
- Vitest + Testing Library + jsdom（组件测试，仅 devDependencies）；
- Playwright（页面 E2E）。

不引入运行时 UI 组件库、表格库或表单库：基础组件用原生 HTML + Tailwind + `clsx` / `tailwind-merge`
与自建 Icon 组件；表格用自定义 DataTable + 服务端分页；表单用受控组件 + 轻量校验。决策见
[0012-web-minimal-stack-and-component-tests.md](../20-adr/0012-web-minimal-stack-and-component-tests.md)。

### 目录结构

```text
web/src/
├── app/            应用组合：providers / router / guards / error boundary
├── pages/          路由入口，只做页面组合
├── features/       业务模块：queries / mutations / 业务组件
├── components/
│   ├── ui/         通用基础组件
│   ├── layout/     AppShell / Sidebar / Topbar / PageHeader
│   ├── data/       DataTable / Pagination / FilterBar / JsonViewer / StatusBadge
│   └── feedback/   EmptyState / ErrorState / Skeleton / ConfirmDialog / Toast
├── api/            唯一 HTTP 出口、DTO 与错误归一化
├── stores/         Zustand：侧栏、主题、表格密度等 UI 状态
├── lib/            cn / format / dates / status / query-keys
├── styles/         Tailwind 与设计令牌
└── test/           Vitest 组件测试 setup 与工具
```

`web/src/features/` 与 `web/src/app/` 是对现有目录规划的补充：页面只负责路由组合，业务 hooks 与业务组件放在 feature 内，
避免所有逻辑堆在 `web/src/pages/`。

### 依赖规则

```text
app → pages → features → components
                     ↘ api → lib
```

- `web/src/components/` 不得引用 `web/src/features/` 或 `web/src/pages/`；
- `web/src/api/` 不得引用 UI；页面不得直接 `fetch`（[AGENTS.md](../../AGENTS.md) 红线 13）；
- 服务端状态只存在 TanStack Query；Zustand 不复制服务端数据；
- 筛选、分页与排序写入 URL，刷新与回退可恢复；
- 只保留一套样式体系：新增组件优先复用现有令牌与 `web/src/components/ui`。

### 路由与页面

| 路由 | 页面 | 主要数据 | 规格 |
| --- | --- | --- | --- |
| `/login` | 登录 | `POST /api/auth/login` | [0001-auth-login-users.md](../40-specs/0001-auth-login-users.md) |
| `/` | 总览 | `GET /api/overview` | [0004-overview-dashboard.md](../40-specs/0004-overview-dashboard.md) |
| `/channels` | 渠道列表 | `GET /api/channels` | [0003-channel-management.md](../40-specs/0003-channel-management.md) |
| `/channels/:id` | 渠道详情 | 渠道详情、暂停/恢复 | [0003-channel-management.md](../40-specs/0003-channel-management.md) |
| `/tasks` | 任务列表 | `GET /api/tasks` | 本页 |
| `/tasks?task=:id` | 任务详情 Sheet | 任务详情、取消 | 本页 |
| `/nodes` | 节点状态 | `GET /api/nodes` | 本页 |
| `/raw-data` | 原始数据浏览（页面待补） | `GET /api/raw-data/sources` | [0002-raw-data-browser.md](../40-specs/0002-raw-data-browser.md) |
| `/raw-data/sources/:id` | 原始数据详情（页面待补） | 详情与 raw | [0002-raw-data-browser.md](../40-specs/0002-raw-data-browser.md) |
| `/settings/users` | 账号管理 | `/api/users` | [0001-auth-login-users.md](../40-specs/0001-auth-login-users.md) |
| `/settings/profile` | 修改密码 | `POST /api/auth/change-password` | [0001-auth-login-users.md](../40-specs/0001-auth-login-users.md) |
| `*` | 404 | 无 | 本页 |

- 除 `/login` 外都经过 `RequireAuth`；未登录由服务端返回 401/302，前端路由守卫作为第二道。
- 页面使用路由级 `lazy`；Recharts、原始数据详情与 JsonViewer 独立拆包。
- 任务详情优先用 `?task=` 侧滑 Sheet，保留列表筛选与滚动位置；如后续需要深链，再加 `/tasks/:id` 页面。

### UI 设计系统

**视觉方向**：中性色高密度控制台，单一品牌蓝作主色；细边框、轻阴影、克制动效。状态颜色只表达语义，
不作为唯一信息载体。保留现有 OKLCH 令牌，扩展成功/警告/信息、图表与状态色。

**布局**：

```text
┌──────────────┬────────────────────────────────────┐
│ Sidebar      │ Topbar：面包屑 / 搜索 / 环境 / 用户 │
│ 240 / 64px   ├────────────────────────────────────┤
│              │ PageHeader：标题 / 操作 / 更新时间   │
│ 渠道          │                                    │
│ 任务          │ 内容区：卡片 / 表格 / 图表          │
│ 节点          │ padding 24–32px                    │
│ 原始数据      │                                    │
│ 设置          │                                    │
└──────────────┴────────────────────────────────────┘
```

- 桌面端侧栏可折叠到 64px；小于 1024px 改为抽屉；表格横向滚动而不是压缩列。
- 总览使用 12 栅格：KPI 卡片 + 趋势/分布图 + 最近任务 + 节点状态。
- 字体：系统 UI 字体栈；ID、record_key、时间与 JSON 使用等宽字体与 `tabular-nums`。
- 间距 4px 基准、8px 节奏；圆角 8–12px；阴影只用于浮层与卡片层级。

**组件分层**：

- `ui/`：Button、Input、Select、Badge、Dialog、Sheet、Tooltip、Tabs、Switch、Toast；
- `layout/`：AppShell、Sidebar、Topbar、PageHeader、Breadcrumb；
- `data/`：DataTable、Pagination、FilterBar、StatusBadge、JsonViewer、CopyButton；
- `feedback/`：EmptyState、ErrorState、Skeleton、ConfirmDialog、LoadingOverlay。

**状态表达**：任务状态、渠道状态与节点状态统一映射为颜色 + 文案 + 图标/圆点；禁止只靠颜色区分。
暗色主题通过 `data-theme` 切换，保留跟随系统选项；状态色与图表色共用令牌。

**交互与可访问性**：

- 所有可操作元素可键盘到达，焦点环使用 `--ring`；Dialog/Sheet 做焦点回收与 `Esc` 关闭。
- 图标按钮必须有 `aria-label`；表格有 caption/aria 描述；表单错误与字段关联。
- 动效 150–200ms，尊重 `prefers-reduced-motion`；不使用无意义动画。
- 颜色对比度满足 WCAG AA；暗色主题单独检查。

## 核心流程

### 应用启动与认证

1. `AuthGate` 请求 `/api/auth/me`，成功则保存当前用户与内存中的 CSRF token。
2. 失败 401 时跳转 `/login?returnUrl=...`；登录成功后失效 `auth/me` 并按 `returnUrl` 返回。
3. 写请求从内存取 token，通过 `X-CSRF-TOKEN` 发送；不把 token 写入 localStorage。
4. 账号被禁用或会话失效时，下一次请求 401 会清理登录态并回到登录页。

### 数据请求与缓存

- 页面通过 feature hooks 调用 `web/src/api/endpoints/`，不直接拼 URL 或 `fetch`。
- Query key 统一由 `web/src/lib/query-keys.ts` 生成，避免字符串散落。
- 列表使用服务端分页与筛选；翻页使用 `placeholderData` 保持旧页，避免闪烁。
- 写操作成功只失效相关列表/详情；不做写操作乐观更新；`409` 重新拉取并提示状态已变化。
- 查询默认重试 1 次，4xx 不重试；mutation 默认不重试。

### 错误与登录态

- `401` 统一清理登录态并跳转登录页；登录接口本身除外。
- `400 invalid_csrf` 重新获取 `/api/auth/me`，只重试一次。
- `403`、`404`、`409`、`5xx` 与网络错误使用统一错误组件或 toast；错误信息不暴露堆栈。
- 页面必须有加载、空、错误、无权限/登录过期状态；不得把错误当空数据。

### 时间展示

- 后端保持 `timestamptz` 与 UTC，API 返回 ISO 8601 `Z` 时间；业务逻辑不做时区转换。
- 前端默认按 `Asia/Shanghai` 展示，允许用户在设置中切换展示时区；时区只影响格式化。
- 列表使用相对时间 + hover 绝对时间；详情显示绝对时间与原始 UTC；时间筛选转换为 UTC 后请求。
- 时区设置属于 UI 偏好，不写入服务端业务字段。

## 数据与接口

### API 客户端

- `web/src/api/client.ts` 是唯一 HTTP 出口：同源 `/api`、`credentials: 'include'`、JSON 解析、
  CSRF 注入、错误归一化、取消信号与 401 处理。
- `web/src/api/errors.ts` 定义 `ApiError`（status、code、message），页面按 code/status 决定展示方式。
- DTO 类型集中在 `web/src/api/types.ts` 或 `web/src/api/endpoints/`；与 admin 契约保持同名、同语义。
- 契约稳定后评估由 OpenAPI 生成类型；在此之前手写 DTO，变更时必须同步本文与对应规格。

### admin 接口依赖

- 认证、账号、渠道、任务、节点与健康检查接口已在 admin 实现；具体行为见 [auth.md](auth.md) 与 [admin.md](admin.md)。
- 原始数据浏览与总览端点设计见 [0002-raw-data-browser.md](../40-specs/0002-raw-data-browser.md) 与
  [0004-overview-dashboard.md](../40-specs/0004-overview-dashboard.md)；实现完成前页面使用空态占位，不造 mock 数据。
- `raw` 只按纯文本/JSON 展示，不渲染 HTML，不使用 `dangerouslySetInnerHTML`；详情提供复制/下载。

## 配置

- 开发期 Vite 将 `/api` 代理到 admin REST 端口，端口分配见 [dev-workflow.md](../30-guides/dev-workflow.md)「端口」。
- 生产同源部署：`scripts/build-web.sh` 把 `web/dist` 复制到 admin 的 `wwwroot`，前端不需要代理。
- 前端只读取 `VITE_` 前缀的公共配置；不持有数据库连接、服务端密钥或节点凭据。
- 展示时区默认 `Asia/Shanghai`，用户偏好存本地；服务端时间字段保持 UTC。

## 测试与守门

- **纯逻辑**：Vitest 覆盖 `web/src/api/client.ts` 错误映射与 CSRF 重试、`web/src/lib/status.ts` 映射、`web/src/lib/format.ts`、query keys、
  Zustand store 行为。
- **组件**：当前 18 个测试文件 / 40 个用例，覆盖 `web/src/components/ui`、`web/src/components/data`、布局与反馈组件的渲染、
  交互、加载、空、错误与禁用态；每个新增组件必须补同目录测试。
- **页面 E2E**：默认 Playwright 8 条用例覆盖登录与 `returnUrl`、未登录守卫、总览、渠道暂停与任务取消确认；业务 API 在用例内
  mock，保持门禁离线。另有 `web/tests/live/` 真实后端用例：真实 Chromium 访问 admin 托管产物，API 直连 admin 与 PostgreSQL，
  覆盖登录/核心页面、渠道启停和任务取消，通过 `pnpm test:e2e:live` 手动执行。
- **门禁**：继续使用 `pnpm verify` 与 `./scripts/verify.sh`；首屏体积预算为 450 kB / 140 kB gzip，较骨架期上调 50/20 kB，
  原因是 Router、Query、基础组件与核心页面进入首屏；数字以 [bundle-budget.json](../../web/bundle-budget.json) 为准，脚本见
  [check-bundle-budget.mjs](../../web/scripts/check-bundle-budget.mjs)。
- 新增页面必须有路由级 E2E 或组件测试，至少覆盖主路径与错误态；不得用截图快照代替行为断言。
- ESLint 同时启用 jsdoc 规则：导出成员必须有 JSDoc，且 `@param`/`@returns`/类型/标签与实际签名一致；C# 后端由 XML 文档编译检查参数名。
- 测试分层与当前门禁见 [testing.md](../30-guides/testing.md)「当前门禁与目标门禁」。

## 待确认事项

| 事项 | 说明 |
| --- | --- |
| 图表交互 | Recharts 图表类型、联动与大数据量降级策略 |
| 前端 i18n | 是否需要多语言；若需要先登记 ADR |
| 富文本 / HTML 注入策略 | 当前禁止 `dangerouslySetInnerHTML`；如后续确有富文本需求，先登记白名单净化方案 |
| DTO 生成 | admin 契约稳定后是否由 OpenAPI 生成 TypeScript 类型 |

## 相关文档

- [system-overview.md](system-overview.md)：模块边界与数据入口；
- [auth.md](auth.md)、[admin.md](admin.md)：认证行为与管理 API；
- [0001-auth-login-users.md](../40-specs/0001-auth-login-users.md)、
  [0002-raw-data-browser.md](../40-specs/0002-raw-data-browser.md)、
  [0003-channel-management.md](../40-specs/0003-channel-management.md)、
  [0004-overview-dashboard.md](../40-specs/0004-overview-dashboard.md)：页面验收标准；
- [0012-web-minimal-stack-and-component-tests.md](../20-adr/0012-web-minimal-stack-and-component-tests.md)：前端栈与测试决策；
- [testing.md](../30-guides/testing.md)：测试分层与门禁；
- [dev-workflow.md](../30-guides/dev-workflow.md)：开发服务器、构建与 E2E 命令；
- [roadmap.md](../00-overview/roadmap.md)：M5 里程碑。
