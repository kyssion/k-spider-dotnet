# k-spider web 技术文档

> 状态：草案
> 最后更新：2026-11-09

本文定义前端技术选型、目录结构、页面路由、API 对接与前端测试边界；当前只有最小可运行页面，
业务页面待实现，模块状态见根 [README.md](../../README.md) 状态表。认证接口、admin API、数据语义与
平台架构见本文「相关文档」。

## 定位与职责

顶层展示与操作界面：通过同源 admin REST API 获取数据与执行操作；不直连数据库，不承载业务规则。认证与权限由 admin 承载，前端负责登录态展示、路由守卫与 CSRF token 携带。

当前 `web/` 有 `App.tsx` 骨架页、入口 `main.tsx`、基础样式、`web/src/lib/cn.ts` 工具函数与 Playwright 冒烟；
ESLint / Prettier / Vitest / 体积预算已接入，业务页面与 API 客户端尚未接入。

## 结构与约定

### 技术选型

已确定（见 [package.json](../../web/package.json)）：

- React 19 + TypeScript + Vite；
- Tailwind CSS 4；
- React Router；
- TanStack Query（服务端状态）；
- Zustand（客户端状态）；
- Recharts（图表）；
- Playwright（E2E）。

选型理由、版本约束与升级策略待补；不引入新的运行时依赖前先确认。

### TypeScript 与构建

- `tsconfig.json` 当前开启 `strict`、`noUnusedLocals`、`noUnusedParameters` 与 `noFallthroughCasesInSwitch`；
- `pnpm build` 先执行 `tsc --noEmit`，类型错误直接阻断构建；
- 新增编译选项或放宽严格性前，先评估对现有代码与契约类型的影响；
- ESLint / Prettier / 组件测试尚未接入，门禁现状与目标见
  [testing.md](../30-guides/testing.md)「当前门禁与目标门禁」。

### 目录规划

| 目录 | 职责 |
|---|---|
| `web/src/api/` | 同源 API 客户端与 DTO 类型（待创建） |
| `web/src/components/` | 通用组件（待创建） |
| `web/src/pages/` | 页面组件（待创建） |
| `web/src/stores/` | Zustand 客户端状态（待创建） |
| `web/src/lib/` | 无业务语义的工具（如 `cn` 类名合并） |
| `web/tests/` | Playwright E2E |

当前有 `App.tsx`、`main.tsx`、`index.css`、`web/src/lib/cn.ts` 与 `tests/smoke.spec.ts`；新增目录时同步本文与
[docs/README.md](../README.md) 的目录索引（如涉及文档）。

### 页面与路由

首期目标页面：登录 / 用户管理、总览、渠道管理、任务控制、节点状态、原始数据只读浏览、账号设置。后续：分析看板、派生表管理、数据重放，依赖 process / 分析层契约。

路由、权限、空态与错误态待补；页面范围在首期 MVP 确认后更新本文。

### UI 规范

Tailwind 设计令牌、组件分层、布局、暗色模式、可访问性、表格/表单/图表约定待补。新增组件优先复用现有令牌与基础组件，不引入第二套样式体系。

## 核心流程

### API 对接

- 同源 `/api`；开发期 Vite 代理到 admin，端口分配见
  [dev-workflow.md](../30-guides/dev-workflow.md)「端口」（配置见 [vite.config.ts](../../web/vite.config.ts)）；
- 请求收敛在 `web/src/api/` 层；页面不直接 `fetch` 的禁令与例外见 [AGENTS.md](../../AGENTS.md) 红线 13；
- DTO 类型与后端契约对齐；分页、筛选、错误处理、CSRF 与登录态跳转在 `web/src/api/` 层统一处理；
- admin REST 尚未定稿，首期页面实现前先确认端点与 DTO。

### 状态管理

TanStack Query 管理服务端状态（缓存、刷新、失效）；Zustand 管理筛选与 UI 客户端状态。不要把服务端数据复制到全局 store。

### 错误与登录态

- `401` 统一清理登录态并跳转登录页；
- `403`、`404`、`5xx` 与网络错误按统一错误组件展示；
- CSRF token 从 `/api/auth/me` 获取，写请求统一附带；
- 登录成功后按 `returnUrl` 返回。

## 数据与接口

- 数据入口的模块边界见 [system-overview.md](system-overview.md)「数据与接口」；
- 认证与账号接口见 [auth.md](auth.md)；admin 管理接口见 [admin.md](admin.md)；
- 查询参数、分页与筛选以 admin 契约为准；在契约未定前不先造 DTO；
- 图表与表格的字段映射以数据模型的语义为准，不把展示层字段当作业务字段。

## 配置

- 开发期代理：`vite.config.ts` 将 `/api` 指向 admin 的开发监听端口，端口分配见
  [dev-workflow.md](../30-guides/dev-workflow.md)「端口」；
- 生产同源部署：构建产物由 `scripts/build-web.sh` 复制到 `src/k-spider-admin/wwwroot`，不需要代理；
- 环境变量与构建模式待补；前端不得持有服务端凭据。

## 测试与守门

- 当前 E2E：`web/tests/smoke.spec.ts`，Playwright 配置见 [playwright.config.ts](../../web/playwright.config.ts)；
- E2E 的业务 API 在用例内按需 mock，保证门禁离线确定；真实后端用例后续补 Live spec；
- 当前静态门禁：`tsc --noEmit`、ESLint、Prettier、Vitest（纯逻辑）与首屏体积预算；体积预算数字以
  [bundle-budget.json](../../web/bundle-budget.json) 为准，脚本见
  [check-bundle-budget.mjs](../../web/scripts/check-bundle-budget.mjs)；
- 组件测试（jsdom / Testing Library）尚未引入，首个真实页面落地时评估，决策见
  [0006-frontend-static-gates.md](../20-adr/0006-frontend-static-gates.md)；
- 新增页面必须有路由级 E2E 或组件测试，至少覆盖主路径与错误态。

## 待确认事项

| 事项 | 说明 |
|---|---|
| admin REST 首期端点/DTO | 由 admin 定义；web 实现前确认 |
| 首期页面范围 | 登录/用户管理优先，其余页面按 MVP 排期 |
| 组件库与样式 | 是否引入组件库；Tailwind 令牌与组件分层 |
| 图表交互 | Recharts 的图表类型、筛选联动与性能边界 |
| 权限粒度 | 当前所有登录用户同权；是否需要页面级权限 |
| 前端 i18n | 是否需要多语言；若需要，先登记 ADR |
| 组件测试范围 | 纯逻辑用 Vitest；是否引入 jsdom / Testing Library 待首个页面评估 |
| 富文本 / HTML 注入策略 | 当前不使用 `dangerouslySetInnerHTML`；如需富文本渲染，先登记待确认并引入白名单净化 |

## 相关文档

- [system-overview.md](system-overview.md)、[auth.md](auth.md)、[admin.md](admin.md)、[data-model.md](data-model.md)：数据入口与契约；
- [dev-workflow.md](../30-guides/dev-workflow.md)：开发服务器、构建与 E2E 命令；
- [roadmap.md](../00-overview/roadmap.md)：M5 里程碑。
