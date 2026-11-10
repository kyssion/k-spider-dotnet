# 测试与门禁

> 状态：持续更新
> 最后更新：2026-11-10
本文范围：测试分层、当前与目标门禁、Live 用例、夹具、验证闭环。命令清单见 [dev-workflow.md](dev-workflow.md)。

说明「什么测试放在哪里、哪些进出门禁、哪些只能手动跑」。红线「不得删除、跳过或弱化测试」见
[AGENTS.md](../../AGENTS.md)。

## 测试分层

| 层 | 位置 | 是否进门禁 | 说明 |
| --- | --- | --- | --- |
| 文档四检查 | `scripts/check-doc-*.mjs` | 是 | 引用、重复、结构、状态；判据见 [docs/README.md](../README.md) |
| 技能清单检查 | `scripts/check-skills-manifest.mjs` | 是 | 受管技能与磁盘一致 |
| 后端服务测试 | 各服务测试项目（admin 已落地；crawler/process 待落地） | 是 | 领域规则、状态机、REST/gRPC 契约 |
| v2 架构 / 契约守卫 | `tests/k-spider-guards/` | 是 | 解决方案布局、DDL 幂等与表清单、CPM/编译门禁配置 |
| gRPC 契约 | `tests/k-spider-grpc-tests/` | 是 | proto 消息 round-trip；字段定稿后补字段 / 错误码 / 流控 |
| admin 核心测试 | `tests/k-spider-admin-tests/` | 是 | 任务状态机、schedule、同渠道互斥、任务生命周期、对账重派/放弃、DI 图 |
| v1 过渡测试 | `src/k-spider-test/` | 是 | 排除 `TestCategory=Live`；crawler/process 服务测试落地后替换 |
| 前端纯逻辑 | `web/src/**/*.test.ts` | 是 | Vitest；工具函数、格式化等可脱离 DOM 的逻辑 |
| 前端组件 | `web/src/**/*.test.tsx` | 是 | Vitest + Testing Library + jsdom；每个组件覆盖渲染、交互与加载/空/错误/禁用态 |
| 前端 E2E | `web/tests/` | 是 | Playwright；当前为骨架冒烟，API 在用例内 mock |
| Live 连通性 | 按测试项目约定 | 否 | 依赖真实接口或线上可达，手动执行 |

### 当前门禁与目标门禁

下表是门禁判据与当前快照；**进度勾选以 [roadmap.md](../00-overview/roadmap.md) 为准**。
当前后端服务测试由 admin 核心测试与 v1 过渡测试共同承担；crawler/process 的 v2 服务测试仍待 M3/M4。
避免把 v1 通过误读为 v2 已验证。

| 门禁 | 当前 | 目标 / 后续 |
| --- | --- | --- |
| 后端服务测试 | admin 核心测试 + v1 过渡测试（离线，排除 Live） | crawler/process 随 M3/M4 建立 v2 服务测试 |
| 架构 / 契约守卫 | `tests/k-spider-guards/`（解决方案、DDL、依赖基线） | 增加服务分层与错误码守卫 |
| 编译质量 | `TreatWarningsAsErrors` + 分析器 + `EnforceCodeStyleInBuild`（v2 项目） | 保持 |
| 依赖可复现 | CPM + `nuget.config` + 传递依赖钉版 | 保持 |
| 前端静态门禁 | `tsc --noEmit` + ESLint + Prettier + Vitest（纯逻辑 + 组件）+ 体积预算 | 保持；新增组件继续补测试 |
| 前端 E2E | Playwright 8 条核心路径 | 保留；新增页面补主路径与错误态 |
| 依赖审计 | 后端 NuGet 审计随 restore / build；CI 跑 `pnpm audit` | 保持 |
| 文档 / 技能 | 4 个文档检查 + 技能清单 | 保持 |

新增前端测试依赖已按 [0006-frontend-static-gates.md](../20-adr/0006-frontend-static-gates.md) 登记；
新增工具仍按 [AGENTS.md](../../AGENTS.md) 红线 10 先确认。

### v2 测试项目接入要求

当前已建立 `tests/k-spider-guards/`、`tests/k-spider-grpc-tests/` 与 `tests/k-spider-admin-tests/`；crawler/process 服务测试项目按同一要求落地。
测试项目落地时必须同时满足以下条件，否则视为“只在本机跑过”：

- 加入 `k-spider-dotnet.sln`，`dotnet build`、`dotnet format` 与 `dotnet test` 都能覆盖；
- 默认离线，Live 用例用 `TestCategory=Live` 标记并在门禁命令中排除；
- 夹具使用接口真实响应，登记来源与抓取时间，不含真实凭据；
- 每个用例独立准备状态，不依赖执行顺序或前一条用例的残留；
- 服务测试至少覆盖：状态机、写入模式、双键水位、异常边界、契约字段；
- 新增测试项目在同一次改动内更新本文与 [dev-workflow.md](dev-workflow.md)「常用命令」。

### 架构与契约守卫

当前 `tests/k-spider-guards/` 已覆盖解决方案布局、DDL 幂等与表清单、CPM / 编译门禁配置；
`tests/k-spider-grpc-tests/` 已覆盖 proto 消息 round-trip；`tests/k-spider-admin-tests/` 已覆盖 admin 状态机、
同渠道互斥、任务生命周期、对账重派/放弃与 DI 图；真实 DB 条件更新与 crawler/process 服务测试随 M2–M4 补。
v2 的架构守卫与业务测试同仓同门禁，至少覆盖：

- **模块依赖方向**：admin / crawler / process 不引用彼此的内部实现，只依赖契约；
- **DDL 与文档**：DDL 幂等、COMMENT / CHECK 完整，且语义与 [data-model.md](../10-architecture/data-model.md) 的规则一致；
- **错误码 / 状态码**：新增错误码或状态必须同时登记权威文档与测试；
- **文档引用**：由 verify 的四检查承担，不在服务测试里重复实现。

守卫只断言“契约有没有被破坏”，不复制契约正文；判据仍以权威文档为准。

## 测试命名

- C# 测试沿用「被测对象 / 方法 + 场景」的 PascalCase 命名，例如 `PartitionEmptyList`、`GetJsonRoundTrip`；
  新服务测试落地后沿用同一风格；
- Playwright 用例用中文描述用户可见行为，例如「控制台骨架可渲染」；
- Vitest 用例沿用中文 `describe` / `it`，只覆盖可脱离 DOM 的纯逻辑；
- 一个用例只断言一个行为；失败信息要能区分期望与实际。

## 断言约定

- 断言**可观察行为**：状态码、错误码、字段、状态机与写入次数；不要断言中文文案、日志或实现细节；
- 失败路径要断言「没有发生写操作」或「状态未推进」，不能只断言返回错误；
- 不写同义反复的断言（例如 `Assert.True(result.IsSuccess)` 后又断言同一个布尔值）；
- 数据访问异常与数据本身失败分开断言：`KDbException` 不消耗业务重试次数；
- E2E 断言用户可见结果与路由，不用快照复述 DOM 结构；页面覆盖要求见
  [web.md](../10-architecture/web.md)「测试与守门」。

## 夹具

- 夹具使用接口真实响应，新增夹具必须登记来源与抓取时间；v1 夹具登记见
  [TestData/README.md](../../src/k-spider-test/TestData/README.md)，v2 夹具位置与登记方式待确认；
- 夹具不得包含真实凭据、Cookie 或个人数据；
- Live 用例用 `TestCategory=Live` 标记，门禁命令显式排除。

## 测试隔离

- 每条用例独立准备状态，不依赖执行顺序或前一条用例的残留；
- 宿主或仓储存在共享单例时，每个用例自建实例，避免跨用例污染；
- 时间、随机数等外部不确定性用可控替身；默认门禁不依赖真实网络与真实账号。

## 门禁边界

- 本地与 CI 的门禁边界、跳过开关见 [dev-workflow.md](dev-workflow.md)「脚本行为」；
- 新服务测试项目落地后，必须加入解决方案与门禁，不能只留在本机；
- v2 守卫项目已在 `.sln` 与 verify / CI 中；v1 测试项目是过渡门禁，crawler/process 服务测试落地后替换，不长期保留双轨。

## 验证闭环

1. 先确定测试层：领域规则、服务契约、接口集成、前端 E2E 或 Live；
2. 写失败用例（或先确认现有用例能复现问题）；
3. 实现修复或功能；
4. 跑对应测试，再跑 `./scripts/verify.sh`；
5. 交付说明列出命令与原始输出，以及未覆盖项。

## 契约与数据

- 后端契约测试以 DDL、REST/gRPC 契约和 [data-model.md](../10-architecture/data-model.md) 的规则为准；
- 前端 E2E 的真实后端用例后续补 Live spec，与后端 `TestCategory=Live` 的分层对齐；
- 文档与技能检查不依赖网络，可在离线环境运行。

## 配置与凭据

- 测试所需的环境变量、数据库连接与凭据由本地或 CI 注入，不写入仓库；
- Live 用例默认不读取生产凭据。

## 不允许的做法

- 删除用例、加跳过标记或注释断言来让构建变绿 —— 判据见 [AGENTS.md](../../AGENTS.md) 红线 1；
- 把真实凭据、生产地址或不可控网络依赖带进默认门禁；Live 用例显式排除；
- 用覆盖率数字代替关键不变式与契约覆盖；
- 集成测试断言易漂移的文案，而不是稳定的状态码 / 错误码 / 字段。

## 待确认事项

- crawler/process 服务测试项目的命名、目录与是否引入统一测试夹具库（admin 已用 `tests/k-spider-admin-tests/`）；
- 组件测试的覆盖率目标与页面 E2E 的边界；
- Live 用例的触发方式（本地脚本 / 手动 CI job）与凭据来源；
- v1 测试替换的时间点与旧用例迁移范围；
- CI 缓存与耗时调优（ADR 0007 的后续观察项）。

## 相关文档

- [dev-workflow.md](dev-workflow.md)：命令、端口与脚本行为；
- [add-a-feature.md](add-a-feature.md)：改动时如何补测试；
- [data-model.md](../10-architecture/data-model.md)、[distributed.md](../10-architecture/distributed.md)、
  [auth.md](../10-architecture/auth.md)：契约测试的权威来源；
- [definition-of-done.md](../50-ai/definition-of-done.md)：交付前自检。
