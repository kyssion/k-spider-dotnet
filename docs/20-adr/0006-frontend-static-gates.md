# 0006 前端静态门禁采用 ESLint + Prettier + Vitest + 体积预算

> 状态：已确认
> 最后更新：2026-11-09
> 决策人：项目所有者（执行指令）
> 关联：[web.md](../10-architecture/web.md)、[testing.md](../30-guides/testing.md)、[dev-workflow.md](../30-guides/dev-workflow.md)
> 影响：`web/package.json`、`web/pnpm-workspace.yaml`、`web/eslint.config.js`、`web/.prettierrc.json`、`web/vitest.config.ts`、`web/bundle-budget.json`、`web/scripts/check-bundle-budget.mjs`、`verify.sh`、CI

## 背景

前端此前只有 `tsc --noEmit`、Vite 构建与 Playwright 冒烟；类型错误能拦住，但格式漂移、常见代码陷阱、
可脱离 DOM 的纯逻辑回归和首屏体积增长都不会让门禁红灯。M1.5 需要把这些质量约束交给机器。

## 备选项

| 选项 | 优点 | 代价 |
| --- | --- | --- |
| A. 只保留 `tsc` + E2E | 依赖最少，改动最小 | 格式、lint、纯逻辑与体积问题继续裸露 |
| B. ESLint + Prettier + Vitest + 体积预算（选择） | 覆盖静态检查、格式、纯逻辑回归与体积回归；全部可离线运行 | 新增一组开发依赖；格式与预算需要维护 |
| C. 在 B 之上立即引入 jsdom + Testing Library | 可测组件 DOM 行为 | 当前没有业务组件，先引入会增加维护面与依赖 |

## 决策

选择 B。ESLint 使用 flat config，Prettier 只做格式，Vitest 只覆盖可脱离 DOM 的纯逻辑，
首屏体积预算独立成 `web/bundle-budget.json`；组件测试等到真实页面落地后再评估（备选 C 不在本轮）。

## 后果

- 正面：前端 lint、格式、纯逻辑测试与体积回归进入 verify 与 CI；新增依赖有明确清单。
- 负面（需要接受的代价）：新增 ESLint / Prettier / Vitest 等开发依赖；格式化会产生一次较大 diff；
  体积预算需要随真实需求调整，调高必须在提交说明中给出理由。
- 需要同步修改的位置：`web/package.json`、`web/pnpm-workspace.yaml`、`web/eslint.config.js`、
  `web/.prettierrc.json`、`web/.prettierignore`、`web/vitest.config.ts`、`web/bundle-budget.json`、
  `web/scripts/check-bundle-budget.mjs`、[web.md](../10-architecture/web.md)、[testing.md](../30-guides/testing.md)、
  [dev-workflow.md](../30-guides/dev-workflow.md)、[verify.sh](../../scripts/verify.sh)、CI。
- 触发重新评估的条件：出现组件级回归需要 DOM 测试；或 lint / 测试 / 格式检查显著拖慢本地与 CI。

## 待办

- [x] ESLint、Prettier、Vitest 与体积预算接入 verify / CI
- [x] 前端依赖审计接入 CI（`pnpm audit`）
- [x] 前端依赖审计清零（Vite / source-map-js / minimatch）
- [ ] 组件测试范围（jsdom / Testing Library）在首个真实页面落地时评估
