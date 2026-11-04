# AGENTS.md — k-spider 开发指南

本文件面向 AI 编码代理（ZCode / Claude Code / Cursor / Copilot Agent 等）。开始任何工作前先读本文件；文档索引与各主题的唯一权威见 [docs/README.md](docs/README.md)。

## 平台是什么

平台定位、模块清单与状态以 [README.md](README.md) 为唯一来源，本文只写开发规则。架构边界见 [docs/architecture.md](docs/architecture.md)，数据规则见 [docs/data-model.md](docs/data-model.md)，功能清单见 [docs/features.md](docs/features.md)。

## 核心原则（最高优先级，覆盖以下所有条目）

1. **机制先行，领域克制**：领域无关的机制（调度/存储/抓取/管线/监控/分析引擎）按"已知会持续增加的变化点"提前抽象；领域知识（源实现、字段映射）保持克制，不为假想源加分支；
2. **契约即边界**：模块间只依赖契约（实体/接口），不依赖彼此实现细节；
3. **数据与执行状态分离**：业务行只存数据语义；执行进度以 `exec_status` 显式表达，重试细节进 `extra`；执行引擎状态可重建、可重放，不另存不可恢复的进程态；
4. **一切皆注册**：源、数据集、管线、Job、分析维度、派生表、告警规则都是注册表条目，平台能力自动生效，**无手写分支**；
5. **放开度量，管住成本**：不设硬编码小窗口/只读/禁物化限制，用查询预算、物化血缘、资源配额等可度量手段管成本；
6. **可读性优先**：抽象不为炫技；实现按最佳实践（惯用法、成熟模式）优先，不为直白而放弃惯用的好写法；敢于使用语言与平台的最新特性，不因"新"而回避；
7. **契约定立须确认**：契约（数据模型契约、基座间契约、表设计规范、管线形态契约、公共接口等）的制定与修改必须先向项目所有者提案并获确认，不得自行拍板；未确认前只能以"待确认"草案形式存在，不得当作既定事实写进代码或文档正文；
8. **先方案后执行**：动手前先出方案（目标、做法、影响面）向项目所有者提交，确认后执行；有拿不准的点主动讨论，不带猜测落地、不自行拍板。

## 项目结构

- `src/`：crawler / process / admin 三个 .NET 服务；`web/`：前端项目（构建产物由 admin 托管）；
- `.agents/skills/`：项目级通用 Agent Skills（模型/agent 无关），说明见 [docs/skills.md](docs/skills.md)；
- `src/k-spider-test/`：v1 测试与夹具，门禁过渡，待新测试替换；
- `src/k-spider-dotnet-old/`：v1 代码、部署单元与旧库导出的归档，仅作考古参照；生产代码不依赖，v1 测试过渡期临时引用；
- 完整结构与模块状态以 [README.md](README.md) 为唯一来源；模块边界与依赖方向见 [docs/architecture.md](docs/architecture.md)。

## 数据模型契约

业务表语义与规则唯一权威是 [docs/data-model.md](docs/data-model.md)，物理结构以 `db/001_business_tables.sql` 为准。系统设施表语义见 [docs/distributed.md](docs/distributed.md)（物理结构 `db/002`），认证行为见 [docs/auth.md](docs/auth.md)（物理结构 `db/003`）。表设计必须满足 data-model 的规则，本文件不再复述。

## 代码约定

- 注释、日志、commit message 用中文；注释解释**为什么**，不复述代码在做什么；
- Job / Stage / 引擎一律 async：网络调用直接 `await`，不要 `.Result` / `.Wait()`；
- 事务写法：`BeginTran → 业务 → CommitTran`，catch 中 `RollbackTran`，不要在 finally 里回滚；
- 批量写入集中在数据访问层，按 write_mode 三模式实现；目标选型为 linq2db（尚未引入依赖，实现前需确认；overwrite=`OnConflictKeyDoUpdate` / skip=`OnConflictDoNothing` / append=普通 Insert），DDL runner、双键水位、对账扫描走裸 SQL；异常包装为 `KDbException`；
- 数据访问异常（`KDbException`）与数据本身失败分开：前者不消耗重试计数；
- 源适配器必须线程安全：游标与页状态走方法参数与返回值，不用可变实例字段；
- 分布式契约（proto + 生成代码）放 `src/k-spider-grpc/`（待创建），admin / crawler 共同引用；各服务不共享实体项目；
- 新增 NuGet 包：`Directory.Packages.props` 加 `PackageVersion` + 项目 csproj 加无版本 `PackageReference`；
- 测试夹具用接口真实响应（不手搓 JSON），新增夹具登记来源与抓取时间；真实接口连通性用例标注 Live 分类，门禁排除；
- 技能文件遵循 Agent Skills 格式；`.agents/skills/` 下由 `manifest.json` 记录的 .NET 技能由同步脚本维护，不直接手改，自定义技能另建目录；
- 凭据红线：不向仓库提交真实凭据；环境相关值走配置/Options，部署方环境变量注入。

## 命令

```bash
dotnet build k-spider-dotnet.sln             # 构建（零警告零错误）
./scripts/verify.sh                          # 一键验证（CI 同款）
dotnet run --project src/k-spider-crawler    # 抓取服务
dotnet run --project src/k-spider-admin      # 调度与后台
./scripts/sync-dotnet-skills.sh             # 同步 .NET Agent Skills 到 .agents/skills
```

配置优先级（唯一说明）：`appsettings.json → appsettings.{环境}.json → K_SPIDER__ 前缀环境变量 → 代码默认值`。

## 当前状态与待定事项

- 模块状态、待实现项以 [README.md](README.md) 状态表为唯一来源；
- 设计待定项见各主题文档的“待定”章节；
- v1 的代码、部署单元与旧库导出已归档到 `src/k-spider-dotnet-old/`，不作现行依据；v1 测试与夹具暂留 `src/k-spider-test/`（门禁过渡，待新测试替换）。
