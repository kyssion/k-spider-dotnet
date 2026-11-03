# k-spider 架构

抓取 → 处理 → 展示三层数据链路，admin 旁路中央调度；平台定位见 [README.md](../README.md)。

**数据链路**：crawler 抓取 → 原始载荷入库（data_source.raw / data_detail.raw）→ process 加工为结构化/格式化数据 → web 前端展示。admin 为旁路控制面：中央调度（任务分发与对账）+ 集群维护（心跳、状态、web 后台），不在数据链路上。

**依赖方向**：web 前端 → admin HTTP API（认证、管理、查询）；admin 旁路调度 crawler / process；crawler 与 process 通过 PostgreSQL 数据契约衔接，互不依赖实现。

## 一、模块

| 层 | 模块 | 项目 | 职责 |
|---|---|---|---|
| 底层 | 抓取 | k-spider-crawler | 原始数据抓取入库；自含数据模型与存储访问 |
| 中间层 | 处理 | k-spider-process | 消费原始载荷，提取加工为结构化/格式化数据 |
| 旁路 | 管理 | k-spider-admin | 中央调度器：任务分发与对账、节点心跳与集群维护；托管 web 前端并承载后台 API |
| 顶层 | 展示 | web/ | 前端页面（Node/pnpm，非 .NET）：展示与操作界面 |

模块状态与待实现项见 [README.md](../README.md) 状态表，本文不重复。

## 二、核心机制

每条机制只在对应主题文档定义，这里只做索引：

- **数据模型**：分类 → 渠道 → 资源 → 内容四张业务表 + 适配器注册表；语义与规则见 [data-model.md](data-model.md)，物理结构见 `db/001_business_tables.sql`；
- **抓取**：crawler 的 hosting / channel / engine / adapter / parser / kernel / common / repository 分层，以及 HttpGateway、反爬验证、write_mode 入库、双键水位，见 [crawler.md](tech/crawler.md)；
- **调度**：admin 中央调度 + 节点执行，gRPC 双向流、任务包、对账与取消，见 [distributed.md](distributed.md)；
- **admin 实现**：服务模块结构、REST 管理 API、前端托管与部署，见 [admin.md](tech/admin.md)；
- **web 前端**：技术选型、目录结构、页面路由与交互约定，见 [web.md](tech/web.md)；
- **认证与账号**：登录、会话、账号管理、CSRF 与配置，见 [auth.md](auth.md)；
- **功能与运维**：数据源清单、抓取/分析/控制台/运维能力见 [features.md](features.md)；
- **处理与分析**：process 设计未开始；目标语义层 / 查询引擎 / 物化按真实需求立项，不预建；v1 的只读分析页面不等同于目标分析引擎。

## 三、测试体系

测试夹具、Live 用例与门禁约定见 [AGENTS.md](../AGENTS.md)；crawler 的测试分层见 [crawler.md](tech/crawler.md)。

## 四、状态与待定

设计状态与待实现项以 [README.md](../README.md) 状态表为准；待定事项见各主题文档的“待定”章节。
