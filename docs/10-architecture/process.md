# k-spider-process 技术文档

> 状态：草案
> 最后更新：2026-11-08
> 权威范围：数据处理服务的边界与待落地设计；数据模型见 [data-model.md](data-model.md)，平台架构见 [system-overview.md](system-overview.md)，节点接入见 [distributed.md](distributed.md)。
> 实现状态：未开始，本文仅为骨架。

## 定位与职责

中间层：消费 crawler 留存的原始载荷（`data_source.raw` / `data_detail.raw`），提取加工为结构化/格式化数据，供分析层与 web 使用。process 不负责抓取，也不在采集侧做深解析；原始数据由 crawler 保留，解析结果由 process 负责。

## 结构与约定

当前未开始，以下为待落地的结构约定，不是现状：

- 解析器/加工器按数据形态注册，机制与具体源知识分离；
- 输入只依赖原始载荷与数据契约，不依赖 crawler 内部实体；
- 输出写入结构化表，表结构与水位规则在“待确认事项”定稿后补入本文与 DDL；
- 将来以 `node_type='process'` 接入 admin 的节点与任务机制，复用 [distributed.md](distributed.md) 的任务包模型。

## 核心流程

目标流程（待实现）：

1. 按双键水位或任务包发现待处理的原始载荷；
2. 选择已注册的解析器/加工器；
3. 解析并写入结构化/格式化数据；
4. 更新消费进度与失败状态；
5. 失败按任务/批次粒度重试，不污染原始数据。

原始载荷的消费方式、加工管线与失败边界在实现前需与项目所有者确认。

## 数据与接口

- 读：`data_source` / `data_detail` 的原始载荷；选择与水位规则待定；
- 写：结构化/格式化数据表，契约（表、字段、索引、水位）尚未定稿；
- 与 crawler 的边界：只通过 PostgreSQL 数据契约衔接，不共享实体项目；
- 与 admin 的边界：节点注册、任务包、状态回报复用 distributed 的协议；
- 与 web/分析层的边界：结构化数据契约定稿后再定义查询接口。

## 配置

配置键、默认值与环境变量随实现补齐；配置优先级与凭据注入规则以 [AGENTS.md](../../AGENTS.md) 为准。未定项在实现前登记，不先写死。

## 测试与守门

- 解析器/加工器的契约测试（离线）；
- 用真实响应夹具做回归；
- Live 用例与门禁分层见 [testing.md](../30-guides/testing.md)；
- 消费水位、失败重试、幂等写入在实现时必须补测试。

## 待确认事项

| 事项 | 说明 |
|---|---|
| 结构化数据契约 | 输出表、字段、水位与增量消费规则（设计前确认） |
| process 节点接入 | 复用 admin 的 `node_type='process'` 与任务包（见 [distributed.md](distributed.md)） |
| 解析器注册与版本 | 解析器编码、版本与回溯/重放语义 |
| 原始载荷消费方式 | 按水位扫描还是按任务包驱动 |

## 相关文档

- [system-overview.md](system-overview.md)、[data-model.md](data-model.md)、[distributed.md](distributed.md)：平台、数据与节点契约；
- [crawler.md](crawler.md)：上游原始载荷生产者；
- [roadmap.md](../00-overview/roadmap.md)：M4 里程碑。
