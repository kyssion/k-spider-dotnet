# k-spider

通用爬虫数据抓取和分析平台：**抓取 → 处理 → 管理**，一条数据链路——原始载荷原样入库，提取加工产出结构化数据，后台统一监控配置。

| 职责 | 项目 | 说明 | 状态 |
|---|---|---|---|
| 抓取（底层） | `k-spider-crawler` | 原始数据抓取入库（data_source.raw / data_detail.raw） | 开发中 |
| 处理（中间层） | `k-spider-process` | 消费原始载荷，提取加工为结构化/格式化数据入库 | 预留 |
| 管理（旁路） | `k-spider-admin` | 控制 crawler 与 process：配置、指令下发、状态监控 | 预留 |
| 展示（顶层） | `k-spider-web`（前端项目） | 组装展示数据 + 前端页面 | 预留 |

数据模型见 [docs/data-model.md](docs/data-model.md)；功能全集见 [docs/features.md](docs/features.md)。

## 项目结构

```
k-spider-dotnet/                     仓库根 = 解决方案根
├── k-spider-dotnet.sln
├── db/                              DDL 与 migration 基线
├── docs/ deploy/ scripts/
├── web/                             展示层前端项目（Node/pnpm）
└── src/                             .NET 后端（三服务）
    ├── k-spider-crawler/            底层：原始数据抓取
    ├── k-spider-process/            中间层：数据加工（预留）
    ├── k-spider-admin/              旁路：管理控制服务
    └── k-spider-dotnet-old/         v1 旧实现归档（不依赖）
```

数据链路：crawler 抓取入库 → process 加工为结构化数据 → web 组装展示；admin 旁路控制 crawler 与 process。

## 快速开始

环境要求：.NET 10、PostgreSQL 15+、Node 20+ / pnpm（web 前端）。

```bash
dotnet build k-spider-dotnet.sln                 # 构建（零警告零错误）
./scripts/verify.sh                              # 一键验证（CI 同款）
psql -d k_spider -f db/001_business_tables.sql   # 初始化业务表（幂等）
dotnet run --project src/k-spider-crawler        # 抓取服务
dotnet run --project src/k-spider-admin          # 管理控制服务
```

配置优先级：`appsettings.json → appsettings.{环境}.json → K_SPIDER__ 前缀环境变量 → 代码默认值`；test/prod 连接串不入库，由部署方环境变量注入。

## 文档

| 文档 | 内容 |
|---|---|
| [AGENTS.md](AGENTS.md) | 开发指南：原则、契约、约定 |
| [docs/architecture.md](docs/architecture.md) | 目标架构：四模块分层、数据链路、核心机制 |
| [docs/data-model.md](docs/data-model.md) | 数据模型：四表 DDL、关键规则、七管线装填对照 |
| [docs/features.md](docs/features.md) | 功能梳理：数据源清单与能力现状 |
