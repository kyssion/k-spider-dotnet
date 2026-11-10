# 0012 web 保持 React 最小栈并采用组件级测试

> 状态：已确认
> 最后更新：2026-11-10
> 决策人：项目所有者（执行指令）
> 关联：[web.md](../10-architecture/web.md)、[testing.md](../30-guides/testing.md)、[0006-frontend-static-gates.md](0006-frontend-static-gates.md)、[0002-raw-data-browser.md](../40-specs/0002-raw-data-browser.md)、[0004-overview-dashboard.md](../40-specs/0004-overview-dashboard.md)
> 影响：`web/package.json`、`web/src/` 目录结构、组件测试、前端依赖清单

## 背景

`web/` 已经是 React 19 + TypeScript + Vite 项目，现有依赖覆盖路由、服务端状态、客户端状态与图表。
前端尚未落地业务页面，组件库、表格库、表单库与组件测试方式都需要在实现前定稿。
项目同时要求“不引入第二套样式体系”，并希望每个组件有详细测试，避免为了快速出页面引入过重的组件依赖。

## 备选项

| 选项 | 优点 | 代价 |
| --- | --- | --- |
| A. 保持 React 最小栈，自建轻量组件，Testing Library + jsdom 做组件测试（选择） | 依赖少、包体小、样式统一；组件行为可被详细测试 | 需要自建并维护 Button/Dialog/DataTable 等组件；可访问性与边界测试成本更高 |
| B. 引入完整组件库（Ant Design / MUI / Chakra） | 页面开发快、组件齐全 | 运行时依赖与包体大；与 Tailwind 令牌形成第二套样式体系，定制成本高 |
| C. 引入 Radix 等 headless primitives + shadcn 风格组件 | 可访问性好，仍可定制样式 | 仍需引入多个运行时依赖；首期页面复杂度不足以抵消成本 |
| D. 不新增任何测试依赖，仅 Vitest 纯逻辑 + Playwright E2E | 依赖最少 | 组件级交互、加载/空/错误/禁用态覆盖不足，不符合“每个组件详细测试” |

## 决策

选择 A。

- 运行时保持 React 19 + TypeScript + Vite + Tailwind CSS 4 + React Router 7 + TanStack Query 5 +
  Zustand 5 + Recharts 3；不引入 UI 组件库、表格库、表单库或 headless UI 库。
- 基础组件用原生 HTML + Tailwind + `clsx` / `tailwind-merge` 与自建 Icon 组件；表格使用自定义
  DataTable + 服务端分页；表单使用受控组件 + 轻量校验。
- 组件测试新增仅 `devDependencies` 的 `@testing-library/react`、`@testing-library/user-event`、
  `@testing-library/jest-dom` 与 `jsdom`；每个 UI/业务组件至少覆盖渲染、交互、加载、空、错误与禁用态，
  页面级主路径与错误态由 Playwright 覆盖。
- 时间字段保持 PostgreSQL `timestamptz`，后端只处理 UTC；前端默认按 `Asia/Shanghai` 展示，允许用户切换，
  时区不进入业务逻辑。
- DTO 先手写在 `web/src/api/`，与 admin 契约同步；OpenAPI 生成类型待 admin 契约稳定后再评估。

## 后果

- 正面：运行时依赖与包体小；样式令牌只有一套；组件行为有测试保护；后续可单独替换某个基础组件。
- 负面（需要接受的代价）：基础组件需要自建与维护；Dialog、Dropdown 等可访问性细节必须自己测试；
  测试依赖增加 CI 时间；没有组件库的“开箱即用”能力；首屏预算从 400/120 kB 调整为 450/140 kB，
  用于容纳 Router、Query、基础组件与核心页面。
- 需要同步修改的位置：`web/package.json`、`web/src/app/`、`web/src/components/`、`web/src/features/`、
  `web/vitest.config.ts`、`web/src/test/`、[web.md](../10-architecture/web.md)、
  [testing.md](../30-guides/testing.md) 与相关规格。
- 触发重新评估的条件：出现无法用自建组件可靠覆盖的可访问性交互；表格或表单复杂度显著超出小型控制台；
  或组件维护成本持续高于引入 headless UI 的成本。
