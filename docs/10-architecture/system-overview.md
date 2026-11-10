# k-spider 架构

> 状态：持续更新
> 最后更新：2026-11-09

本文定义系统分层、模块边界与依赖方向；各机制的细节以对应主题文档为准。平台定位、模块状态与
主要契约见本文「相关文档」。

## 定位与职责

k-spider 是“抓取 → 处理 → 展示”的数据平台。crawler 把外部数据站的原始载荷抓回 PostgreSQL，process 消费原始载荷并产出结构化/格式化数据，web 负责展示与操作；admin 是旁路控制面，负责中央调度、集群维护、认证与前端托管，不在数据链路上。

依赖方向：web → admin HTTP API；admin 旁路调度 crawler / process；crawler 与 process 通过 PostgreSQL 数据契约衔接，互不依赖实现。各模块不得绕过契约直接读取对方的内部实现。

## 结构与约定

### 模块

| 层 | 模块 | 项目 | 职责 |
|---|---|---|---|
| 底层 | 抓取 | `k-spider-crawler` | 原始数据抓取入库；自含数据模型与存储访问 |
| 中间层 | 处理 | `k-spider-process` | 消费原始载荷，提取加工为结构化/格式化数据 |
| 旁路 | 管理 | `k-spider-admin` | 中央调度器：任务分发与对账、节点心跳与集群维护；托管 web 前端并承载后台 API |
| 顶层 | 展示 | `web/` | 前端页面（Node/pnpm，非 .NET）：展示与操作界面 |

### 机制索引

每条机制只在对应主题文档定义，本节只做索引；除 data-model / distributed / auth 的已确认契约外，其余为目标设计或骨架，模块状态以根 [README.md](../../README.md) 为准。

- **数据模型**：分类 → 渠道 → 资源 → 内容四张业务表 + 适配器注册表；语义见 [data-model.md](data-model.md)，物理结构见 [001_business_tables.sql](../../db/001_business_tables.sql)；
- **抓取**：crawler 的 hosting / channel / engine / adapter / parser / kernel / common / repository 分层，以及 HttpGateway、反爬验证、write_mode 入库、双键水位，见 [crawler.md](crawler.md)；
- **调度**：admin 中央调度 + 节点执行，gRPC 双向流、任务包、对账与取消，见 [distributed.md](distributed.md)；
- **admin 实现**：服务模块结构、REST 管理 API、前端托管与部署，见 [admin.md](admin.md)；
- **web 前端**：技术选型、目录结构、页面路由与交互约定，见 [web.md](web.md)；
- **认证与账号**：登录、会话、账号管理、CSRF 与配置，见 [auth.md](auth.md)；
- **功能与运维**：数据源清单、抓取/分析/控制台/运维能力见 [features.md](../00-overview/features.md)；
- **处理与分析**：process 设计未开始；目标语义层 / 查询引擎 / 物化按真实需求立项，不预建；v1 的只读分析页面不等同于目标分析引擎。

### 边界与不变量

本节只做索引，条款的唯一来源是各自文档：

- 模块依赖方向与服务边界：见本文「模块」与各服务技术文档；
- 数据与执行状态、写入规则、水位与时间：见 [data-model.md](data-model.md)「数据与执行状态分离」「写入规则」「水位与时间」；
- 调度状态机、派发 token、对账与取消：见 [distributed.md](distributed.md)「状态机」「防竞态写入」「对账」「重派与取消」；
- 认证、会话与 CSRF：见 [auth.md](auth.md)「结构与约定」「安全」；
- gRPC 契约项目与 proto 草案：见 [distributed.md](distributed.md)「gRPC 双向流」与 [0008-grpc-contract-project.md](../20-adr/0008-grpc-contract-project.md)；
- 各服务不共享实体项目：见本文「模块」与根 [README.md](../../README.md) 项目结构。

## 核心流程

1. admin 读取 `channel.schedule` 规划渠道轮任务包，按节点负载派发给 crawler；
2. crawler 按任务包执行列表轮，把列表条目写入 `data_source`；需要详情时执行详情回填轮，写入 `data_detail`；
3. process 消费原始载荷，按结构化契约加工后写入分析侧数据；
4. web 通过 admin 的同源 REST API 查询数据与执行管理操作；
5. admin 周期对账非终态任务，处理节点失联、任务超时、重派与取消。

流程的详细状态机、重试与失败边界分别见 [distributed.md](distributed.md)、[crawler.md](crawler.md) 与 [process.md](process.md)。

## 数据与接口

- **服务间数据契约**：PostgreSQL 是 crawler 与 process 的主要契约面；业务表语义见 [data-model.md](data-model.md)，系统设施表见 [distributed.md](distributed.md)，认证表见 [auth.md](auth.md)；
- **admin REST API**：web 的唯一数据入口；认证与 CSRF 行为见 [auth.md](auth.md)，端点清单与 DTO 见 [admin.md](admin.md)；
- **gRPC**：admin 与 crawler / process 节点之间的调度通道；协议与端口拆分见 [distributed.md](distributed.md)；
- **物理结构**：三份 DDL 是物理结构唯一权威；文档只维护语义与规则。

## 配置

系统级配置优先级与凭据注入规则以 [AGENTS.md](../../AGENTS.md) 为唯一来源；各服务的配置键、默认值与环境变量见对应技术文档的“配置”章节。平台架构层不引入独立配置项。

## 测试与守门

测试分层、当前与目标门禁、Live 用例与门禁边界见 [testing.md](../30-guides/testing.md)；crawler 的测试分层见
[crawler.md](crawler.md)。文档四检查与技能清单的环节见 [testing.md](../30-guides/testing.md)「测试分层」。

## 待确认事项

跨模块的待确认事项集中登记在 [20-adr/README.md](../20-adr/README.md)；各主题细节见对应文档的“待确认事项”章节。架构层尚未定稿的主要有：服务间 gRPC 契约、数据访问选型、process 结构化数据契约与部署形态。

## 相关文档

- [vision.md](../00-overview/vision.md)：平台定位、目标与非目标；
- [roadmap.md](../00-overview/roadmap.md)：里程碑与暂不考虑事项；
- [data-model.md](data-model.md)、[distributed.md](distributed.md)、[auth.md](auth.md)：三大契约面；
- 根 [README.md](../../README.md)：模块落地状态与仓库结构。
