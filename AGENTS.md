# AGENTS.md — k-spider AI 协作入口

> 任何 AI 代理在本仓库开始工作前，必须先读本文件。
> 详细规范见 [docs/README.md](docs/README.md)；本文件只保留“不读就会出错”的部分。

## 1. 这是什么

k-spider 是“抓取 → 处理 → 展示”的数据平台。平台定位、模块清单与状态以 [README.md](README.md) 为唯一来源；本文只写协作规则。

当前处于 v2 重建期：契约和文档先行，服务代码从骨架逐步落地。模块状态只看根 README 状态表。

## 2. 项目结构

| 路径 | 内容 |
|---|---|
| `src/k-spider-crawler` | 原始数据抓取与入库 |
| `src/k-spider-process` | 原始载荷加工（未开始） |
| `src/k-spider-admin` | 中央调度、gRPC、REST API、认证、前端托管 |
| `web/` | React/Vite 前端（骨架，待接入 admin API） |
| `db/` | 现行 DDL 基线（001 业务 / 002 系统 / 003 认证） |
| `docs/` | 唯一权威知识源：overview / architecture / adr / guides / specs / ai |
| `.agents/skills/` | 任务触发的领域知识，按需加载 |
| `src/k-spider-test/` | v1 测试与夹具，门禁过渡 |
| `src/k-spider-dotnet-old/` | v1 归档，仅考古参照 |

## 3. 命令

```bash
dotnet build k-spider-dotnet.sln             # 构建（零警告零错误）
./scripts/verify.sh                          # 全量校验
node scripts/check-doc-references.mjs        # 文档引用 / 状态 / 索引检查
dotnet run --project src/k-spider-crawler    # 抓取服务
dotnet run --project src/k-spider-admin      # 调度与后台
node scripts/sync-dotnet-skills.mjs          # 同步 .NET Agent Skills
node scripts/sync-dotnet-skills.mjs --check  # 只检查上游是否有更新
```

配置优先级（唯一说明）：`appsettings.json → appsettings.{环境}.json → K_SPIDER__ 前缀环境变量 → 代码默认值`。

## 4. 红线

1. **不得**删除、跳过或弱化测试，不得为通过构建而注释断言；
2. **不得**修改数据模型、DDL、调度、认证、公共接口等契约而不先登记 ADR/待确认项并同步权威文档；
3. **不得**把目标设计写成现状；未确认的参数、接口、字段只能标【待确认】并登记到 [docs/20-adr/README.md](docs/20-adr/README.md)；
4. **不得**在没有代码出处的情况下写“已实现 / 已验证 / 已支持”；
5. **不得**提交真实凭据；环境相关值走配置/Options；
6. **不得**手改 manifest 记录的上游技能；升级一律走同步脚本；
7. **不得**在自己启动的后台进程未停止时交付（除非用户明确要求保持运行）；
8. **不得**未经明确指示提交、推送或改写远端历史；
9. **不得**声称验证通过而不给出实际命令结果；
10. **不得**引入新依赖或框架而不先确认。

## 5. 工作流

```text
① 定向  AGENTS.md → docs/README.md → 相关 10-architecture 文档
        必要时只加载 1~2 个相关 .agents/skills/ 技能
② 确认  判断改动类型：功能 → 40-specs；结构/契约 → 20-adr
        缺契约或不可逆决策先停止提问，不自行拍板
③ 实现  契约 → 数据访问 → 服务 → 接口/前端；小步提交改动
④ 同步  同一次改动更新文档、状态日期、索引、待确认清单
⑤ 验证  node scripts/check-doc-references.mjs；./scripts/verify.sh 全绿
⑥ 交付  汇总改动/验证/未覆盖项；提交与推送按项目所有者指示
```

## 6. 文档体系

| 目录 | 职责 |
|---|---|
| `docs/00-overview/` | 愿景、功能范围、术语、路线 |
| `docs/10-architecture/` | 系统总览、数据模型、分布式、认证、各服务设计 |
| `docs/20-adr/` | 决策记录、待确认清单、模板 |
| `docs/30-guides/` | 技术文档模板、写作/开发指南 |
| `docs/40-specs/` | 功能规格与验收条件 |
| `docs/50-ai/` | AI 协作模型、Definition of Done、技能体系 |

规则：

- 每份设计文档开头必须有 `> **状态**：...（最后核对：YYYY-MM-DD）`；“已确认”不等于已实现；
- 同一主题只在一处定义，其他文档只链接；新增文档必须加入 [docs/README.md](docs/README.md) 索引；
- 新服务/模块文档用 [30-guides/tech-doc-template.md](docs/30-guides/tech-doc-template.md)；
- 未确认事项登记到 [20-adr/README.md](docs/20-adr/README.md)，确认后写决策记录再改代码；
- 失效文档同一次提交修正，不留旧路径、旧命令、旧状态。

## 7. 技能（按需加载，别读完再动手）

`.agents/skills/` 下平铺 107 个来自 [dotnet/skills](https://github.com/dotnet/skills) 的上游技能，归属和版本见 `.agents/dotnet-skills/manifest.json`。

| 任务 | 常用技能 |
|---|---|
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
3. **无关技能不加载**：Blazor、MAUI、WinForms、.NET 11 等 21 个技能默认与本项目栈无关；
4. **受管技能不手改**：`manifest.json` 记录的上游技能下次同步会覆盖；自有技能另建目录，放在 `.agents/skills/` 同级；
5. 同步：`node scripts/sync-dotnet-skills.mjs`，支持 `--check`、`--ref`、`--source`、`--force`、`--allow-plugin-removal`。

## 8. 核心原则

1. **机制先行，领域克制**：机制按已知变化点抽象；领域知识只落在适配器/解析器；
2. **契约即边界**：模块间只依赖契约，不依赖实现细节；
3. **数据与执行状态分离**：业务行存数据语义，执行进度走 `exec_status`，重试细节进 `extra`；
4. **一切皆注册**：源、数据集、管线、Job、分析维度、派生表、告警规则都可注册，无手写分支；
5. **放开度量，管住成本**：不设硬编码小窗口/只读限制，用预算、血缘、配额管成本；
6. **可读性优先**：抽象不为炫技，优先惯用法和成熟模式；
7. **契约定立须确认**：契约和公共接口变更先提案、确认、登记，未确认不落代码；
8. **先方案后执行**：先给目标、做法、影响面，确认后执行；拿不准就讨论。

## 9. 数据模型契约

业务表语义与规则唯一权威是 [10-architecture/data-model.md](docs/10-architecture/data-model.md)，物理结构以 `db/001_business_tables.sql` 为准。系统设施表语义见 [10-architecture/distributed.md](docs/10-architecture/distributed.md)（物理结构 `db/002_sys_tables.sql`），认证行为见 [10-architecture/auth.md](docs/10-architecture/auth.md)（物理结构 `db/003_auth_tables.sql`）。

## 10. 代码约定

- 注释、日志、commit message 用中文；注释解释**为什么**，不复述代码在做什么；
- Job / Stage / 引擎一律 async，网络调用 `await`，不要 `.Result` / `.Wait()`；
- 事务：`BeginTran → 业务 → CommitTran`，catch 中 `RollbackTran`，不在 finally 回滚；
- 批量写入集中在数据访问层，按 write_mode 三模式实现；目标选型 linq2db（尚未引入依赖，实现前确认；overwrite=`OnConflictKeyDoUpdate` / skip=`OnConflictDoNothing` / append=普通 Insert），DDL runner、双键水位、对账扫描走裸 SQL；异常包 `KDbException`；
- 数据访问异常（`KDbException`）与数据本身失败分开：前者不消耗重试次数；
- 源适配器线程安全：游标与页状态走参数/返回值，不用可变实例字段；
- 分布式契约（proto + 生成代码）放 `src/k-spider-grpc/`（待创建），admin / crawler 共同引用；各服务不共享实体项目；
- 新增 NuGet 包：`Directory.Packages.props` 加 `PackageVersion` + csproj 加无版本 `PackageReference`；
- 测试夹具用接口真实响应，新增夹具登记来源与抓取时间；Live 用例门禁排除；
- 凭据红线：仓库不提交真实凭据；环境值由部署方注入。

## 11. 当前状态与待定事项

- 模块状态以 [README.md](README.md) 状态表为唯一来源；
- 待确认契约见 [docs/20-adr/README.md](docs/20-adr/README.md)；各主题文档的“待定”章节是补充；
- v1 代码/部署/旧库导出归档在 `src/k-spider-dotnet-old/`，不作现行依据；v1 测试暂留 `src/k-spider-test/`。
