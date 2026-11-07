# k-spider

通用爬虫数据抓取和分析平台：**抓取 → 处理 → 展示**，一条数据链路——原始载荷原样入库，提取加工产出结构化数据，web 前端展示。定位、目标与非目标见 [vision.md](docs/00-overview/vision.md)。

| 职责 | 项目 | 状态 |
|---|---|---|
| 抓取（底层） | `k-spider-crawler` | 骨架 |
| 处理（中间层） | `k-spider-process` | 未开始 |
| 管理（旁路） | `k-spider-admin` | 骨架 |
| 展示（顶层） | `web/`（前端项目） | 骨架 |

状态口径：**未开始 / 骨架 / 实现中 / 可用**。模块职责与依赖见 [system-overview.md](docs/10-architecture/system-overview.md)；数据模型见 [data-model.md](docs/10-architecture/data-model.md)；功能清单见 [features.md](docs/00-overview/features.md)；里程碑与下一步见 [roadmap.md](docs/00-overview/roadmap.md)；已知问题见 [issue-tracker.md](docs/00-overview/issue-tracker.md)。

## 常用入口

| 我想知道 | 去看 |
|---|---|
| 项目定位、目标与非目标 | [vision.md](docs/00-overview/vision.md) |
| 里程碑、下一步与暂不考虑事项 | [roadmap.md](docs/00-overview/roadmap.md) |
| 已知问题与处理结论 | [issue-tracker.md](docs/00-overview/issue-tracker.md) |
| 第一次跑通：环境、数据库、启动、验证 | [getting-started.md](docs/30-guides/getting-started.md) |
| 常用命令、端口、脚本、进程收尾、排错 | [dev-workflow.md](docs/30-guides/dev-workflow.md) |
| 测试分层、Live 用例、门禁 | [testing.md](docs/30-guides/testing.md) |
| 新增一个完整特性 | [add-a-feature.md](docs/30-guides/add-a-feature.md) |
| 平台能力与数据源 | [features.md](docs/00-overview/features.md) |
| 系统架构、调度、认证 | [10-architecture/README.md](docs/10-architecture/README.md) |
| 历史决策与待确认事项 | [20-adr/README.md](docs/20-adr/README.md) |
| 贡献流程与提交前检查 | [CONTRIBUTING.md](CONTRIBUTING.md) |
| 安全报告渠道与已知限制 | [SECURITY.md](SECURITY.md) |
| AI 协作红线与代码约定 | [AGENTS.md](AGENTS.md) |
| 文档索引与写作规则 | [docs/README.md](docs/README.md) |

## 项目结构

```
k-spider-dotnet/                     仓库根 = 解决方案根
├── k-spider-dotnet.sln
├── db/                             现行 DDL 基线（001 业务 / 002 系统 / 003 认证，均幂等）
├── deploy/                         部署单元（现行服务待补齐；v1 单元已归档）
├── docs/                           唯一权威知识源（overview / architecture / adr / guides / specs / ai）
├── scripts/
├── .agents/skills/                 Agent Skills 标准技能（跨 agent / 模型共享）
├── web/                            前端项目（Node/pnpm，构建产物目标由 admin 托管，当前未接入）
└── src/
    ├── k-spider-crawler/           底层：原始数据抓取
    ├── k-spider-process/           中间层：数据加工
    ├── k-spider-admin/             旁路：中央调度、集群维护与前端托管
    ├── k-spider-test/              v1 测试与夹具（门禁过渡，待新测试替换）
    └── k-spider-dotnet-old/        v1 代码/部署/旧库导出归档（仅考古，生产不依赖）
```

v1 测试暂留 `src/k-spider-test/`，会临时引用归档项目；新测试落地后移除。

## 快速开始

完整步骤（环境要求、数据库初始化、启动、验证）见 [getting-started.md](docs/30-guides/getting-started.md)。配置约定（含优先级）与凭据注入规则见 [AGENTS.md](AGENTS.md)。

## 技能

通用 Agent Skills 放在 `.agents/skills/`；目录、跨 agent 发现方式与上游同步见 [skills.md](docs/50-ai/skills.md)。

## 文档

文档索引、目录级索引、各主题唯一权威与维护要求见 [docs/README.md](docs/README.md)。
