# DataForge

**通用数据基座 + 数据分析基座**：采集侧以"数据形态管线 × 任意领域源"的架构接入全网数据源，存入 PostgreSQL；分析侧在入库数据之上提供语义查询、聚合物化与派生表能力。两大基座通过存储层契约连接，独立部署、独立演进。

> 命名 `DataForge` 暂定，最终以拍板为准。

## 平台组成

| 基座 | 职责 |
|---|---|
| **数据基座**（Collection） | 调度、源注册中心、管线引擎、抓取内核（含反爬验证）、可观测 |
| **存储层**（Storage） | 数据集/记录统一数据模型、写入语义、变更检测、治理（分区/保留期）、分发 |
| **分析基座**（Analysis） | 语义层（维度/指标/视图）、查询引擎（预算制）、物化引擎、派生表（血缘） |
| **平台控制台**（Console） | 源管理、数据浏览、任务与节点控制、派生表管理、分析看板、监控告警 |

详细设计见 [docs/architecture.md](docs/architecture.md)；功能全集见 [docs/features.md](docs/features.md)。

## 功能总览

- **多源采集**：源适配器 + 四种抓取策略（游标翻页 / 全量对账 / 窗口快照 / 键增量），接入新源 = 一个适配器 + 一行注册；
- **反爬验证**：识别器与通过策略注册表，JS/Cloudflare 类挑战浏览器自动通过并回放会话，过不了的明确告警不静默丢数据；
- **统一存储**：`dataset_record` 统一表 + 投影列 + 分区；写入三模式（追加 / 按键 upsert / 哈希修订）；
- **数据分析**：声明式维度/指标/视图，查询预算制（无硬性时间窗限制），物化视图与派生表（血缘 + 刷新策略）；
- **平台控制台**：schema 驱动的数据浏览、任务控制、节点状态、分析看板；
- **可观测**：注册即探测（接口可用性/新鲜度/积压/反爬状态），告警通道插件化。

## 项目结构

```
k-spider-dotnet/                 仓库根 = 解决方案根（目录名历史遗留，重构时随新命名一并处理）
├── db/                          建库 DDL
├── deploy/                      systemd 服务模板
├── docs/                        架构与功能文档
├── scripts/                     一键验证等脚本
├── web/                         控制台前端源码
└── src/
    ├── DataForge.Abstractions/  契约包：模型 + 接口 + 注册表契约（两基座唯一共同依赖）
    ├── DataForge.Storage/       存储实现：连接/托管 migration/同步引擎/治理
    ├── DataForge.Collection/    数据基座（Exe）：内核/引擎/源适配器/可观测
    ├── DataForge.Analysis/      分析基座（Exe）：语义层/查询/物化/派生表/API
    ├── DataForge.Console/       平台控制台（Exe）
    └── DataForge.Tests/         契约测试/夹具回归/Live 连通性
```

> 以上为重构目标结构；当前仓库仍为重构前的旧代码树（`src/k-spider-*`），重构从 0 落地上述结构。

## 快速开始

环境要求：.NET 10、PostgreSQL 15+、Node 20+ / pnpm（前端）。

```bash
# 初始化数据库
psql -h 127.0.0.1 -U postgres -f db/k_script_spider.sql k_script_spider

# 构建 + 测试
./scripts/verify.sh

# 运行数据基座 / 分析基座 / 控制台
dotnet run --project src/DataForge.Collection
dotnet run --project src/DataForge.Analysis
dotnet run --project src/DataForge.Console
```

多环境配置：`DOTNET_ENVIRONMENT=Development|Test|Production`，读取优先级 `appsettings.json → appsettings.{环境}.json → K_SPIDER__ 前缀环境变量 → 代码默认值`；test/prod 连接串不入库，由部署方环境变量注入。

## 文档

| 文档 | 内容 |
|---|---|
| [AGENTS.md](AGENTS.md) | AI 代理开发指南：原则、约定、扩展套路 |
| [docs/architecture.md](docs/architecture.md) | 目标架构：两大基座、统一数据模型、管线引擎、分析基座 |
| [docs/features.md](docs/features.md) | 功能梳理：采集/存储/分析/控制台/运维能力全集与源清单 |
