# AGENTS.md — k-spider AI 协作入口

> 任何 AI 代理在本仓库开始工作前，必须先读本文。
> 详细规范见 [docs/README.md](docs/README.md)；本文只保留「不读就会出错」的部分：红线、代码约定、配置优先级、工作流入口与技能路由。

## 这是什么

k-spider 是「抓取 → 处理 → 展示」的数据平台。项目定位、目标与非目标见 [vision.md](docs/00-overview/vision.md)；
模块清单、落地状态与仓库结构以根 [README.md](README.md) 为唯一来源；当前处于 v2 重建期，服务代码从骨架逐步落地。

## 项目结构与命令

项目结构与仓库布局以根 [README.md](README.md) 为唯一来源；首次跑通见
[getting-started.md](docs/30-guides/getting-started.md)；常用命令、端口、脚本与进程收尾见
[dev-workflow.md](docs/30-guides/dev-workflow.md)。本文不维护命令表。

配置优先级（唯一说明）：`appsettings.json → appsettings.{环境}.json → K_SPIDER__ 前缀环境变量 → 代码默认值`。

## 红线

1. **不得**删除、跳过或弱化测试，不得为通过构建而注释断言；
2. **不得**修改数据模型、DDL、调度、认证、公共接口等契约而不先登记 ADR/待确认项并同步权威文档；
3. **不得**违反文档规则（目标/现状、待确认、ADR），规则唯一权威见 [docs/README.md](docs/README.md)；
4. **不得**在没有代码出处的情况下写「已实现 / 已验证 / 已支持」；
5. **不得**提交真实凭据；环境相关值走配置/Options；
6. **不得**违反受管技能规则（见 [skills.md](docs/50-ai/skills.md)）；
7. **不得**在自己启动的后台进程未停止时交付（除非用户明确要求保持运行）；
8. **不得**未经明确指示提交、推送或改写远端历史；
9. **不得**声称验证通过而不给出实际命令结果；
10. **不得**引入新依赖或框架而不先确认；
11. **不得**把同一个事实写成两份正文（约定、表格、清单、流程都必须只有一处），需要多处出现时只写引用 ——
    归属查 [docs/README.md](docs/README.md)「内容归属」，精确重复由
    [check-doc-duplication.mjs](scripts/check-doc-duplication.mjs) 在 verify 中拦下。

## 工作流

变更分类、所需产物、强制顺序与文档同步矩阵见
[change-protocol.md](docs/50-ai/change-protocol.md)；角色分工、阶段门禁、上下文管理与升级时机见
[collaboration-model.md](docs/50-ai/collaboration-model.md)；完成清单见
[definition-of-done.md](docs/50-ai/definition-of-done.md)；已知问题见
[issue-tracker.md](docs/00-overview/issue-tracker.md)。

一句话版本：**先定向（读文档）→ 缺产物先补 → 从内到外实现 → `./scripts/verify.sh` 全绿 → 收尾 →
按所有者指示提交/推送**。

## 文档

目录索引、各主题唯一权威、状态块、写作规则与检查命令见 [docs/README.md](docs/README.md)。每个文档目录有自己的
`README.md` 作为文件索引；新增/删除文件时同一次改动更新所属目录 README 和必要的目录索引。
贡献流程见 [CONTRIBUTING.md](CONTRIBUTING.md)，安全报告渠道与已知限制见 [SECURITY.md](SECURITY.md)。

## 交付格式

按 [collaboration-model.md](docs/50-ai/collaboration-model.md) 的四段式汇报（结论 / 改动 / 验证 / 差异与后续），
验证部分必须是实际命令与原始输出。

## 技能（按需加载，别读完再动手）

`.agents/skills/` 下平铺来自 [dotnet/skills](https://github.com/dotnet/skills) 的上游技能；归属、版本、数量与同步规则见
[skills.md](docs/50-ai/skills.md)。

| 任务 | 常用技能 |
| --- | --- |
| C# 语言、重构、构建基础 | `csharp-expert`、`csharp-refactoring`、`msbuild`、`setup-local-sdk` |
| Web API、端点、中间件 | `dotnet-webapi` |
| 测试生成 / 执行 / 覆盖率 / 反模式 | `run-tests`、`code-testing`、`coverage-analysis`、`test-anti-patterns` |
| MSBuild、构建性能 | `build-perf-diagnostics`、`incremental-build`、`binlog-failure-analysis` |
| 性能调查、dump、trace | `analyzing-dotnet-performance`、`dump-collect`、`dotnet-trace-collect` |
| 版本升级、AOT、nullable | `dotnet-aot-compat`、`migrate-nullable-references` |
| 包管理 | `convert-to-cpm` |
| AI / ML | `technology-selection` |

约束：

1. **项目约定优先**：上游技能面向通用 .NET；与本仓库契约、分层、数据访问选型冲突时，以 `docs/` 和 `20-adr/` 为准；
2. **不预读**：一个任务只加载 1~2 个相关技能；
3. **无关技能不加载**：Blazor、MAUI、WinForms、.NET 11 等与本项目栈无关的技能默认不加载；
4. **受管技能不要手改**：manifest 记录了它们，下次同步会覆盖；升级走同步脚本，自有技能另建目录。

## 核心原则

核心原则、非目标与成功标准见 [vision.md](docs/00-overview/vision.md)；本文不重复。

## 数据模型契约

业务表语义与规则唯一权威是 [data-model.md](docs/10-architecture/data-model.md)，物理结构以
[001_business_tables.sql](db/001_business_tables.sql) 为准。系统设施表语义见
[distributed.md](docs/10-architecture/distributed.md)（物理结构 [002_sys_tables.sql](db/002_sys_tables.sql)），
认证行为见 [auth.md](docs/10-architecture/auth.md)（物理结构 [003_auth_tables.sql](db/003_auth_tables.sql)）。

## 代码约定

- 注释、日志、commit message 用中文；注释解释**为什么**，不复述代码在做什么；
- Job / Stage / 引擎一律 async，网络调用 `await`，不要 `.Result` / `.Wait()`；
- 事务：`BeginTran → 业务 → CommitTran`，catch 中 `RollbackTran`，不在 finally 回滚；
- 批量写入集中在数据访问层，按 write_mode 三模式实现；目标选型 linq2db（尚未引入依赖，【待确认】实现前确认；
  overwrite=`OnConflictKeyDoUpdate` / skip=`OnConflictDoNothing` / append=普通 Insert），DDL runner、双键水位、
  对账扫描走裸 SQL；异常包 `KDbException`；
- 数据访问异常（`KDbException`）与数据本身失败分开：前者不消耗重试次数；
- 源适配器线程安全：游标与页状态走参数/返回值，不用可变实例字段；
- 分布式契约（proto + 生成代码）放 `src/k-spider-grpc/`（待创建），admin / crawler 共同引用；各服务不共享实体项目；
- 新增 NuGet 包：`Directory.Packages.props` 加 `PackageVersion` + csproj 加无版本 `PackageReference`；
- 测试夹具用接口真实响应，新增夹具登记来源与抓取时间；Live 用例门禁排除；
- 凭据红线：仓库不提交真实凭据；环境值由部署方注入。

## 归档与待确认

- 待确认契约见 [20-adr/README.md](docs/20-adr/README.md)；各主题文档的「待确认事项」章节是补充；
- v1 代码/部署/旧库导出归档在 `src/k-spider-dotnet-old/`，不作现行依据；v1 测试暂留 `src/k-spider-test/`。
