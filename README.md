# k-spider

通用爬虫数据抓取和分析平台：**抓取 → 处理 → 管理**，一条数据链路——原始载荷原样入库，提取加工产出结构化数据，后台统一监控配置。

| 职责 | 说明 | 状态 |
|---|---|---|
| 抓取 | 接入任意领域数据源，原始载荷与资源登记入 PostgreSQL | 开发中 |
| 处理 | 消费原始载荷，提取加工为结构化/格式化数据 | 预留 |
| 管理 | 爬虫状态监控、渠道配置、指令下发、数据查看 | 预留 |

数据模型见 [docs/data-model.md](docs/data-model.md)；功能全集见 [docs/features.md](docs/features.md)。

> 模块划分与目录结构：重新设计中。

## 快速开始

环境要求：.NET 10、PostgreSQL 15+。

```bash
dotnet build k-spider-dotnet.sln      # 构建（零警告零错误）
./scripts/verify.sh                   # 一键验证（CI 同款）
```

配置优先级：`appsettings.json → appsettings.{环境}.json → K_SPIDER__ 前缀环境变量 → 代码默认值`；test/prod 连接串不入库，由部署方环境变量注入。

## 文档

| 文档 | 内容 |
|---|---|
| [AGENTS.md](AGENTS.md) | 开发指南：原则、契约、约定 |
| [docs/architecture.md](docs/architecture.md) | 平台定位与职责划分 |
| [docs/data-model.md](docs/data-model.md) | 数据模型：四表 DDL、关键规则、七管线装填对照 |
| [docs/features.md](docs/features.md) | 功能梳理：数据源清单与能力现状 |
