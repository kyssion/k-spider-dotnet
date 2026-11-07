# 架构索引

> 状态：持续更新
> 最后更新：2026-11-08

本目录是系统结构、数据模型、分布式、认证与各服务设计的唯一权威所在；跨目录的事实归属见
[docs/README.md](../README.md)「内容归属」。

| 文档 | 回答的问题 |
| --- | --- |
| [system-overview.md](system-overview.md) | 模块边界、依赖方向、核心机制索引 |
| [data-model.md](data-model.md) | 业务表语义、写入规则、水位与治理 |
| [distributed.md](distributed.md) | 节点/任务模型、调度、对账、取消 |
| [auth.md](auth.md) | 认证、会话、账号管理与安全配置 |
| [crawler.md](crawler.md) | 抓取分层、适配器、反爬、仓储 |
| [process.md](process.md) | 数据处理服务 |
| [admin.md](admin.md) | 管理控制服务、REST/gRPC 与前端托管 |
| [web.md](web.md) | 前端展示与操作界面 |

模块落地状态以根 [README.md](../../README.md) 状态表为准；本表不重复维护模块状态。

## 技术文档骨架

本目录**所有**文档按 [tech-doc-template.md](../30-guides/tech-doc-template.md) 的统一八段组织；
顺序与完整性由 [check-doc-structure.mjs](../../scripts/check-doc-structure.mjs) 校验。

```markdown
# <模块> 技术文档

> 状态：草案
> 最后更新：YYYY-MM-DD

## 定位与职责
## 结构与约定
## 核心流程
## 数据与接口
## 配置
## 测试与守门
## 待确认事项
## 相关文档
```

「测试与守门」写本主题相关的机器守卫（测试名 / 脚本名）；「待确认事项」只写本主题细节，
集中索引见 [20-adr/README.md](../20-adr/README.md)。不适用的章节保留标题并写一句「不适用」及原因 ——
删段会让人分不清是「没想清楚」还是「不用写」。
