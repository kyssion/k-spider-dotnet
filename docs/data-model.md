# 数据模型（物理表）

> 业务表表设计规则唯一权威；DDL 以 `db/001_business_tables.sql` 为准（幂等基线），本文不重复维护 DDL。
> 系统设施表（sys_node / sys_task）属分布式层，表设计唯一权威见 [distributed.md](distributed.md)，DDL 基线落 `db/002_sys_tables.sql`。

## 模型

适配器注册 + 分类 → 渠道 → 资源 → 内容四张业务表：

| 表 | 职责 |
|---|---|
| crawler_adapter | 适配器注册：抓取适配器目录，parser 隐式绑定 |
| channel_category | 渠道分类：树形归属（站点→模块→栏目） |
| channel | 数据渠道：一个可拉取的数据供给单元 |
| data_source | 资源登记：每个要抓的资源连接一行 |
| data_detail | 资源内容：资源产出的原始数据，source : detail = 1 : N |

规则：

- 采集库只存原始数据：解析仅限抓取必需（record_key 预映射、详情抓取目标现场解析），标题/业务时间等一切解析产物归分析侧；
- record_key 格式「渠道前缀id:sha256」：预映射与哈希由代码生成、PG 只存；source 键 = 资源唯一性（链接不变行不变），detail 键 = 内容唯一性（可含时间窗）；
- raw（bytea）为未解析原始载荷，双侧留存（source 条目 / detail 全量）；
- 写入规则三模式（渠道配置，source/detail 行级快照）：overwrite 覆盖（upsert）/ skip 不处理（DO NOTHING）/ append 增加（直接插入，要求 record_key 含时间窗）；
- data_status 记数据事实，exec_status 记执行进度，两列独立；执行细节在 extra 约定键（fail_count / next_retry_at / last_error；新增约定键须先与项目所有者确认）；
- adapter 命名：无前缀 = 本表自己的快照，带下游前缀（detail_）= 下游计划；parser 隐式绑定 adapter（驱动抓取的最小解析），不单独配置；
- 适配器目录：crawler_adapter 表登记，业务表存编码，不建外键；
- 枚举列为 text：稳定集合加 CHECK 约束，开放集合（适配器编码）由代码注册表校验；
- 每表三件套 id / create_time / update_time；表列必须 COMMENT ON；
- extra 为表级扩展容器，固定列不够时启用。
