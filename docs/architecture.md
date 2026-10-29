# k-spider 架构

通用爬虫数据抓取和分析平台：抓取 → 处理 → 展示三层数据链路，admin 旁路中央调度。

**数据链路**：crawler 抓取 → 原始载荷入库（data_source.raw / data_detail.raw）→ process 加工为结构化/格式化数据 → web 组装展示。admin 为旁路控制面：中央调度（任务分发与对账）+ 集群维护（心跳与 web 后台），方案见 [distributed.md](distributed.md)，不在数据链路上。

## 一、模块

| 层 | 模块 | 项目 | 职责 | 状态 |
|---|---|---|---|---|
| 底层 | 抓取 | k-spider-crawler | 原始数据抓取入库；自含数据模型与存储访问 | 开发中 |
| 中间层 | 处理 | k-spider-process | 消费原始载荷，提取加工为结构化/格式化数据 | 预留 |
| 旁路 | 管理 | k-spider-admin | 中央调度器：任务分发与对账、节点心跳与集群维护、web 后台（Web API） | 待重建 |
| 顶层 | 展示 | k-spider-web | 组装展示数据 + 前端页面（Node/pnpm，非 .NET） | 预留 |

依赖方向：`web → process → collector（数据基座）`；admin 旁路调度 crawler / process。

## 二、项目结构

```
k-spider-dotnet/
├── k-spider-dotnet.sln
├── db/                           # DDL 与 migration 基线
├── docs/ deploy/ scripts/
├── web/                          # 展示层前端项目（Node/pnpm）
└── src/                          # .NET 后端（三服务）
    ├── k-spider-crawler/         #   底层：原始数据抓取
    ├── k-spider-process/         #   中间层：数据加工（预留）
    └── k-spider-admin/           #   旁路：中央调度与集群维护（待重建，见 distributed.md）
```

命名空间 PascalCase（`KSpider.Crawler / KSpider.Process / KSpider.Admin`）。

## 三、核心机制

- **数据模型**：分类→渠道→资源→内容四张业务表 + 适配器注册表 + write_mode 三模式入库 + source:detail=1:N——规则唯一权威见 [data-model.md](data-model.md)，DDL 落 `db/001_business_tables.sql`；
- **源适配器**：渠道级列表适配器（拉资源、登记 source）+ 资源级详情适配器（产出内容）+ 解析器（纯函数，可离线回归）；一切皆注册——代码注册表按编码取用（未知编码加载即抛），接入新源 = 适配器实现 + 渠道行；
- **管线**：列表轮（资源登记）→ 详情回填轮（按 exec_status 取件）；按渠道 write_mode 入库（三模式），异常按渠道粒度兜底，本轮失败下一轮再来；
- **抓取内核**：HttpGateway 唯一 HTTP 出口（每主机独立客户端、同主机串行 + 最小间隔、失败冷却）；反爬验证 Detector（纯判定注册表，越具体越靠前）→ Solver（代价升序）→ Policy（按源放行集），行为对齐 v1 实测；
- **调度**：admin 统一调度 + 节点执行（方案见 [distributed.md](distributed.md)），尚未实现；
- **可观测**：指标 / 健康探测 / 告警通道插件化，注册即覆盖；
- **处理与分析未启动**：process 的提取加工 / 语义层 / 查询引擎（预算制）/ 物化，按真实需求立项，不预建。

## 四、测试体系

契约测试（离线）/ 夹具回归（真实响应，迁 v1 TestData 不迁代码）/ Live 连通性（门禁排除）。

## 五、待定

| 事项 | 状态 |
|---|---|
| process 设计 | 未开始 |
