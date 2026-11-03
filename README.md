# k-spider

通用爬虫数据抓取和分析平台：**抓取 → 处理 → 展示**，一条数据链路——原始载荷原样入库，提取加工产出结构化数据，web 前端展示。

| 职责 | 项目 | 状态 |
|---|---|---|
| 抓取（底层） | `k-spider-crawler` | 开发中 |
| 处理（中间层） | `k-spider-process` | 预留 |
| 管理（旁路） | `k-spider-admin` | 待重建 |
| 展示（顶层） | `web/`（前端项目） | 骨架已重建，待实现 |

模块职责与依赖见 [docs/architecture.md](docs/architecture.md)；数据模型见 [docs/data-model.md](docs/data-model.md)；功能清单见 [docs/features.md](docs/features.md)。

## 项目结构

```
k-spider-dotnet/                     仓库根 = 解决方案根
├── k-spider-dotnet.sln
├── db/                             现行 DDL 基线（001 业务 / 002 系统 / 003 认证，均幂等）
├── deploy/                         部署单元（现行服务待补齐；v1 单元已归档）
├── docs/ scripts/
├── web/                            前端项目（Node/pnpm，构建产物由 admin 托管）
└── src/
    ├── k-spider-crawler/           底层：原始数据抓取
    ├── k-spider-process/           中间层：数据加工
    ├── k-spider-admin/             旁路：中央调度、集群维护与前端托管
    ├── k-spider-test/              v1 测试与夹具（门禁过渡，待新测试替换）
    └── k-spider-dotnet-old/        v1 代码/部署/旧库导出归档（不依赖）
```

数据链路与 admin 调度方案见 [docs/architecture.md](docs/architecture.md) 与 [docs/distributed.md](docs/distributed.md)。

## 快速开始

环境要求：.NET 10、PostgreSQL 15+、Node 20+ / pnpm（web 前端）。

```bash
dotnet build k-spider-dotnet.sln                 # 构建（零警告零错误）
./scripts/verify.sh                              # 一键验证（CI 同款）
psql -d k_spider -f db/001_business_tables.sql   # 初始化业务表（幂等）
psql -d k_spider -f db/002_sys_tables.sql        # 初始化系统设施表
psql -d k_spider -f db/003_auth_tables.sql       # 初始化认证表
dotnet run --project src/k-spider-crawler        # 抓取服务
dotnet run --project src/k-spider-admin          # 调度与后台
```

配置约定（含优先级）与凭据注入规则见 [AGENTS.md](AGENTS.md)。

## 文档

文档索引、各主题唯一权威与维护要求见 [docs/README.md](docs/README.md)。
