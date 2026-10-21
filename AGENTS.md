# AGENTS.md — k-spider 开发指南

本文件面向 AI 编码代理（ZCode / Claude Code / Cursor / Copilot Agent 等）。开始任何工作前先读本文件与 [docs/architecture.md](docs/architecture.md)。

## 平台是什么

**通用爬虫数据抓取和分析平台**：抓取侧接入任意领域数据源、原始载荷入 PostgreSQL；处理侧对原始数据提取加工为结构化/格式化数据；管理侧统一监控配置与查看。数据模型见 [docs/data-model.md](docs/data-model.md)，功能全集见 [docs/features.md](docs/features.md)。

## 核心原则（最高优先级，覆盖以下所有条目）

1. **机制先行，领域克制**：领域无关的机制（调度/存储/抓取/管线/监控/分析引擎）按"已知会持续增加的变化点"提前抽象；领域知识（源实现、字段映射）保持克制，不为假想源加分支；
2. **契约即边界**：模块间只依赖契约（实体/接口），不依赖彼此实现细节；
3. **数据与进程状态分离**：业务行只存数据语义；抓取进度、阶段状态、重试计数归执行引擎（可重建、可重放）；
4. **一切皆注册**：源、数据集、管线、Job、分析维度、派生表、告警规则都是注册表条目，平台能力自动生效，**无手写分支**；
5. **放开度量，管住成本**：不设硬编码小窗口/只读/禁物化限制，用查询预算、物化血缘、资源配额等可度量手段管成本；
6. **可读性优先**：抽象不为炫技；实现按最佳实践（惯用法、成熟模式）优先，不为直白而放弃惯用的好写法；敢于使用语言与平台的最新特性，不因"新"而回避；
7. **契约定立须确认**：契约（数据模型契约、基座间契约、表设计规范、管线形态契约、公共接口等）的制定与修改必须先向项目所有者提案并获确认，不得自行拍板；未确认前只能以"待确认"草案形式存在（如 data-model.md 待拍板表），不得当作既定事实写进代码或文档正文；
8. **先方案后执行**：动手前先出方案（目标、做法、影响面）向项目所有者提交，确认后执行；有拿不准的点主动讨论，不带猜测落地、不自行拍板。

## 数据模型契约（表设计必须满足）

1. 业务行只存数据语义；执行进度以 `exec_status` 列显式表达（pending/running/failed/success），失败计数、重试时间等执行细节记录在行内扩展字段（extra 约定键），可重建、可重放；
2. 去重键稳定：source 按 `(channel_id, record_key)`（资源唯一性）、detail 按 `(source_id, record_key)`（内容唯一性，1:N），record_key 格式「渠道前缀id:sha256」由代码生成；
3. 写入语义唯一：覆盖式 upsert——source 按 `(channel_id, record_key)`、detail 按 `(source_id, record_key)`；历史快照由 detail 内容键含时间窗表达；
4. 采集库只存原始数据：解析仅限抓取必需（record_key 预映射、详情抓取目标），标题/业务时间等解析产物归分析侧；
5. 治理能力：按 create_time 等价分批归档 + 保留期声明；
6. 原始响应可独立归档，但解析重放窗口内必须可达。

## 代码约定

- 注释、日志、commit message 用中文；注释解释**为什么**，不复述代码在做什么；
- Job / Stage / 引擎一律 async：网络调用直接 `await`，不要 `.Result` / `.Wait()`；
- 事务写法：`BeginTran → 业务 → CommitTran`，catch 中 `RollbackTran`，不要在 finally 里回滚；
- 批量写入：手拼 `ON CONFLICT`（绕开 ORM 批量分页限制），实现集中在数据访问层，异常包装为 `KDbException`；
- 数据访问异常（`KDbException`）与数据本身失败分开：前者不消耗重试计数；
- 源适配器必须线程安全：游标与页状态走方法参数与返回值，不用可变实例字段；
- 新增 NuGet 包：`Directory.Packages.props` 加 `PackageVersion` + 项目 csproj 加无版本 `PackageReference`；
- 测试夹具用接口真实响应（不手搓 JSON），新增夹具登记来源与抓取时间；真实接口连通性用例标注 Live 分类，门禁排除；
- 凭据红线：不向仓库提交真实凭据；环境相关值走配置/Options，部署方环境变量注入。

## 命令

```bash
dotnet build k-spider-dotnet.sln      # 构建（零警告零错误）
./scripts/verify.sh                   # 一键验证（CI 同款）
```

配置优先级：`appsettings.json → appsettings.{环境}.json → K_SPIDER__ 前缀环境变量 → 代码默认值`。

## 当前状态与待定事项

- 模块划分与目录结构**重新设计中**（此前设计已废弃清理，不留存档）；
- **物理表设计**：已定稿，见 [docs/data-model.md](docs/data-model.md)（DDL 待落 `db/` migration 基线）；
- **平台命名**：已拍板 `k-spider`。
