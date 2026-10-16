# AGENTS.md — DataForge 开发指南

本文件面向 AI 编码代理（ZCode / Claude Code / Cursor / Copilot Agent 等）。开始任何工作前先读本文件与 [docs/architecture.md](docs/architecture.md)。

## 平台是什么

**通用数据基座 + 数据分析基座**：采集侧以"数据形态管线 × 任意领域源"接入全网数据源，存入 PostgreSQL；分析侧在入库数据之上提供语义查询、物化与派生表能力。两大基座通过存储层契约连接，独立部署、独立演进。目标架构见 [docs/architecture.md](docs/architecture.md)，功能全集见 [docs/features.md](docs/features.md)。

> 重构授权：本项目从 0 完全重写、完全推翻原逻辑，**不承接任何历史实现**；旧实现与旧文档在 git 历史中，需要考古时自行查阅，但**不得把旧结构/旧约定带进新代码**。

## 核心原则（最高优先级，覆盖以下所有条目）

1. **机制先行，领域克制**：领域无关的机制（调度/存储/抓取/管线/监控/分析引擎）按"已知会持续增加的变化点"提前抽象；领域知识（源实现、字段映射）保持克制，不为假想源加分支；
2. **契约即边界**：基座之间只依赖契约包（`DataForge.Abstractions`），不依赖彼此实现；
3. **数据与进程状态分离**：业务行只存数据语义；抓取进度、阶段状态、重试计数归执行引擎（可重建、可重放）；
4. **一切皆注册**：源、数据集、管线、Job、分析维度、派生表、告警规则都是注册表条目，平台能力自动生效，**无手写分支**；
5. **放开度量，管住成本**：不设硬编码小窗口/只读/禁物化限制，用查询预算、物化血缘、资源配额等可度量手段管成本；
6. **可读性优先**：抽象不为炫技；宁要直白的长代码，不要绕弯的短代码。

## 项目结构

```
src/
├── DataForge.Abstractions/     # 契约包：模型 + 接口 + 注册表契约（两基座唯一共同依赖）
├── DataForge.Storage/          # 存储实现：连接工厂/托管 migration/同步引擎/治理
├── DataForge.Collection/       # 数据基座（Exe）
│   ├── Kernel/                 #   抓取内核（反爬验证）、限流、代理池
│   ├── Engine/                 #   调度引擎 + 管线引擎
│   ├── Sources/                #   源适配器（按域分目录）
│   └── Observer/               #   指标/健康探测/告警
├── DataForge.Analysis/         # 分析基座（Exe）：语义层/查询引擎/物化/派生表/API
├── DataForge.Console/          # 平台控制台（Exe）
├── DataForge.Tests/            # 契约测试/夹具回归/Live 分层
└── web/                        # 控制台前端
```

依赖方向：`Collection / Analysis / Console → Abstractions →（运行时）Storage`；源适配器只依赖 Abstractions + Kernel。命名空间 PascalCase（`DataForge.*`）。

## 数据模型契约（表设计必须满足）

1. 业务行只存数据语义，进程状态外置到 `sys_*`；
2. 去重键稳定：`(dataset_id, record_key)`，record_key 选源内稳定形态；
3. 写入语义只能是三模式之一：`Append` / `UpsertByKey` / `Revisioned`（哈希条件更新）；
4. `event_time`（数据业务时间）与 `ingest_time`（入库时间）分离；
5. 治理能力：按 event_time 分区（或等价分批归档）+ 保留期声明；
6. 原始响应可独立归档，但解析重放窗口内必须可达。

## 代码约定

- 注释、日志、commit message 用中文；注释解释**为什么**，不复述代码在做什么；
- Job / Stage / 引擎一律 async：网络调用直接 `await`，不要 `.Result` / `.Wait()`；
- 事务写法：`BeginTran → 业务 → CommitTran`，catch 中 `RollbackTran`，不要在 finally 里回滚；
- 批量写入：手拼 `ON CONFLICT`（绕开 ORM 批量分页限制），实现集中在 Storage 层，异常包装为 `KDbException`；
- 数据访问异常（`KDbException`）与数据本身失败分开：前者不消耗重试计数；
- 源适配器必须线程安全：游标与页状态走方法参数与返回值，不用可变实例字段；
- 新增 NuGet 包：`Directory.Packages.props` 加 `PackageVersion` + 项目 csproj 加无版本 `PackageReference`；
- 测试夹具用接口真实响应（不手搓 JSON），新增夹具登记来源与抓取时间；真实接口连通性用例标注 Live 分类，门禁排除；
- 凭据红线：不向仓库提交真实凭据；环境相关值走配置/Options，部署方环境变量注入。

## 扩展套路

| 要加什么 | 怎么做 |
|---|---|
| 新数据源 | 写一个 `ISourceAdapter`（Descriptor + FetchStrategy + Mapper）→ 在源注册中心注册一行；抓取策略覆盖不了的新形态才扩展 `IFetchStrategy` |
| 新反爬验证识别方式 | `VerificationKind` 加值 → 写 `IVerificationDetector`（纯判定、不联网）→ 注册表按"越具体越靠前"插入 → 补夹具与用例 |
| 新反爬通过手段 | 写 `IVerificationSolver`（声明 Kinds 与 Cost）→ 注册表加一行；人机确认类不进默认放行集，按源显式放开 |
| 新抓取阶段的加工 | 实现一个 Stage 挂进 Pipeline，不新开 Job |
| 新分析维度 | 语义层注册 Dimension/Metric/AnalysisView → 查询引擎自动覆盖 → 前端图表配置 |
| 新派生表 | 登记定义/血缘/构建策略/刷新节奏/保留期，由物化引擎调度 |
| 新告警规则/通道 | 规则注册 + 通道插件（日志/飞书/Webhook） |
| 需要浏览器渲染的页面 | 抓取内核的浏览器渲染服务（按需启动），不进常规 HTTP 链路 |

## 命令

```bash
dotnet build k-spider-dotnet.sln                                          # 构建（零警告零错误）
dotnet test src/DataForge.Tests --filter "TestCategory!=Live"             # 离线门禁
./scripts/verify.sh                                                       # 一键验证（CI 同款）
dotnet run --project src/DataForge.Collection                             # 数据基座
dotnet run --project src/DataForge.Analysis                               # 分析基座
dotnet run --project src/DataForge.Console                                # 控制台（默认 5800）
DOTNET_ENVIRONMENT=Test dotnet run --project src/DataForge.Collection     # 切换环境
dotnet publish src/DataForge.Collection -c Release -r linux-x64 --self-contained -p:SpiderEnvironment=Production
```

配置优先级：`appsettings.json → appsettings.{环境}.json → K_SPIDER__ 前缀环境变量 → 代码默认值`。

## 当前状态与待定事项

- 重构从 0 开始：契约包 → 内核/引擎 → 单源端到端 → 全源适配 → 分析基座 → 控制台；
- **物理表设计**：设计中（受上文数据模型契约约束）；
- **调度引擎选型**：待定（持久化调度库 vs 自研抢占式；v1 单机形态两者均够用）；
- **平台命名**：暂定 `DataForge`，目录名 `k-spider-dotnet` 为历史遗留，随命名拍板一并处理。
