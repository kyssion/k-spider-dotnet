# 数据模型（业务表）

> 业务表语义与规则唯一权威；物理结构唯一权威是 `db/001_business_tables.sql`（列/约束/索引/COMMENT）。
> 系统设施表：sys_node / sys_task 语义见 [distributed.md](distributed.md)，物理结构落 `db/002_sys_tables.sql`；sys_user 行为见 [auth.md](auth.md)，物理结构落 `db/003_auth_tables.sql`。
>
> **状态**：已确认（最后核对：2026-11-04）。

## 模型

适配器注册 + 分类 → 渠道 → 资源 → 内容四张业务表：

| 表 | 职责 |
|---|---|
| crawler_adapter | 适配器注册：抓取适配器目录，parser 隐式绑定 |
| channel_category | 渠道分类：树形归属（站点→模块→栏目） |
| channel | 数据渠道：一个可拉取的数据供给单元 |
| data_source | 资源登记：每个要抓的资源连接一行 |
| data_detail | 资源内容：资源产出的原始数据，source : detail = 1 : N；行存在 = 详情已抓取 |

### 术语

| 术语 | 含义 | 对应 |
|---|---|---|
| 源站 / 站点 | 外部数据站点 | `channel_category` 根节点 |
| 渠道 | 一个可拉取的数据供给单元 | `channel` |
| 适配器 | 列表 / 详情的抓取实现目录 | `crawler_adapter` |
| 资源 / 列表条目 | 一条待抓取的资源连接 | `data_source` |
| 详情内容 | 一个资源产出的原始内容 | `data_detail` |
| 数据集 / 管线 | v1 概念；v2 不建独立实体 | 由 `channel` + adapter 表达 |

## 规则

- 业务行只存数据语义；执行进度以 `exec_status` 显式表达，重试细节在 `extra` 约定键；执行引擎状态可重建、可重放；
- 采集库只存原始数据：解析仅限抓取必需（record_key 预映射、详情抓取目标现场解析），标题/业务时间等一切解析产物归分析侧；
- record_key 格式「渠道前缀id:sha256」的精确规则【待确认】：前缀与 id 来源、hash 输入（source = 规范化链接；detail = 内容键 + 时间窗）、时间窗格式与时区；预映射与哈希由代码生成、PG 只存；source 键 = 资源唯一性（链接不变行不变），detail 键 = 内容唯一性（可含时间窗）；
- data_source 不设独立 URL 列：资源定位与详情抓取目标由 raw + parser 现场解析，record_key 负责资源去重；
- raw（bytea）为未解析原始载荷，双侧留存（source 条目 / detail 全量）；在 `channel.retention_days` 声明的保留期内必须可达，超出保留期后可归档或清理；
- 写入规则三模式（渠道配置，source/detail 行级快照）：overwrite 覆盖（upsert）/ skip 不处理（DO NOTHING）/ append 增加（直接插入，要求 record_key 含时间窗）；detail 写入使用 source 行快照还是当前 channel 配置【待确认】；
- data_status 记数据事实，exec_status 记执行进度，两列独立；执行细节在 extra 约定键（fail_count / next_retry_at / last_error；新增约定键须先与项目所有者确认）；filtered / dead / paywalled 是否跳过详情回填【待确认】；
- 分发采用双键水位：消费方按 `(update_time, id)` 升序扫描；新行和更新行都通过 `update_time` 进入水位，id 用于同一时间戳内的稳定排序；
- update_time 由写入方显式刷新；基线 DDL 不建触发器，overwrite / append / 状态更新都不得依赖 `DEFAULT now()` 代替更新；
- 治理：按 create_time 范围分批归档 + 保留期声明（channel.retention_days）；首期不分区；
- `channel.schedule` 与 `channel.extra` 的 JSON 键、类型、时区尚未定稿，分别见 [tech/admin.md](tech/admin.md) 调度章节与 [tech/crawler.md](tech/crawler.md) 配置章节；实现前确认；
- data_detail.channel_id 必须等于所属 data_source.channel_id；数据库不强制，由 repository 写入时保证；
- data_detail 行存在 = 详情已抓取；失败尝试只记在 data_source.exec_status / extra。若未来允许失败 detail 行，须先修改本契约与 DDL CHECK；
- adapter 列语义：`adapter` = 产出本行的适配器编码快照；`detail_adapter` = 详情抓取适配器编码；两者都存编码，不建外键；parser 隐式绑定 adapter（驱动抓取的最小解析），不单独配置；
- 适配器目录：crawler_adapter 表登记，业务表存编码，不建外键；
- 枚举列为 text：稳定集合加 CHECK 约束，开放集合（适配器编码）由代码注册表校验；
- 每表三件套 id / create_time / update_time；表列必须 COMMENT ON；
- extra 为表级扩展容器，固定列不够时启用。
