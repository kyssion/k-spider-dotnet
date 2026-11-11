# 0013 注释一致性检查进入 verify 门禁

> 状态：已确认
> 最后更新：2026-11-10
> 决策人：项目所有者（执行指令）
> 关联：[AGENTS.md](../../AGENTS.md)、[testing.md](../30-guides/testing.md)、[definition-of-done.md](../50-ai/definition-of-done.md)、[0010-admin-modular-monolith.md](0010-admin-modular-monolith.md)、[0012-web-minimal-stack-and-component-tests.md](0012-web-minimal-stack-and-component-tests.md)
> 影响：`web/eslint.config.js`、`web/package.json`、`Directory.Build.props`、`tests/k-spider-guards/`、`scripts/verify.sh`

## 背景

前端与后端都要求实现代码补注释，但注释会随签名和实现漂移：改了参数名、返回值或状态机，
注释仍描述旧行为。人工评审能发现语义错误，但无法保证每次改动都检查；需要在现有 lint / build
门禁中增加可重复的结构一致性检查。

## 备选项

| 选项 | 优点 | 代价 |
| --- | --- | --- |
| A. 前端 ESLint jsdoc + 后端 XML 文档编译检查（选择） | 复用现有 lint/build，无需新脚本；参数/返回值/标签漂移直接红灯 | 仅能检查结构；语义描述仍需评审；新增前端 dev 依赖 |
| B. 自研注释解析脚本 | 可按项目规则定制 | 需维护 TS/C# 两套解析，容易与真实编译器脱节 |
| C. 只做人工评审 | 零依赖 | 无法保证每次改动都检查，正是当前问题 |

## 决策

选择 A。

- 前端新增仅 `devDependencies` 的 `eslint-plugin-jsdoc`，在 ESLint 中启用
  `require-jsdoc`、`check-param-names`、`check-tag-names`、`check-types`、`valid-types`、`check-syntax` 与
  `require-returns-check`；`src/api`、`src/lib` 额外要求 `@param`/`@returns` 及描述。
- 后端在 `Directory.Build.props` 开启 `GenerateDocumentationFile`；`CS1591` 只对 gRPC 生成代码与测试项目豁免，
  业务项目缺失公共成员 XML 文档会直接阻断。`TreatWarningsAsErrors` 同时把 `<param>` 名称不匹配
  （CS1572/CS1573）与畸形 XML（CS1570）变成构建错误。
- `tests/k-spider-guards/CommentConsistencyTests.cs` 守卫这两项配置不被静默移除。
- `./scripts/verify.sh` 的前端 lint 与后端构建步骤即执行检查；语义层面“注释描述是否与实现行为一致”
  仍由 DoD 人工评审，不能用结构检查替代。

## 后果

- 正面：每次改动自动检查参数名、返回值、JSDoc 标签与签名；配置被删除时守卫测试会红灯。
- 负面（需要接受的代价）：新增 `eslint-plugin-jsdoc` devDependency；业务项目公共成员必须有 XML 文档；
  结构检查不能发现“注释说 401、代码返回 403”这类语义漂移；注释维护成本上升。
- 需要同步修改的位置：`web/package.json`、`web/eslint.config.js`、`Directory.Build.props`、
  `tests/k-spider-guards/`、`scripts/verify.sh`、[AGENTS.md](../../AGENTS.md)、
  [testing.md](../30-guides/testing.md)、[definition-of-done.md](../50-ai/definition-of-done.md)。
- 触发重新评估的条件：jsdoc 规则误报率过高；需要语义级检查；或前端注释规范发生变化。
