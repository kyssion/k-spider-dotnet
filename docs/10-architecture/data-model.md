# 数据模型（业务表）

> 状态：已确认
> 最后更新：2026-11-08
> 权威范围：业务表语义与规则；物理结构唯一权威是 [001_business_tables.sql](../../db/001_business_tables.sql)（列、约束、索引、COMMENT）。
> 系统设施表语义见 [distributed.md](distributed.md)（物理结构 [002_sys_tables.sql](../../db/002_sys_tables.sql)）；认证表行为见 [auth.md](auth.md)（物理结构 [003_auth_tables.sql](../../db/003_auth_tables.sql)）。术语见 [glossary.md](../00-overview/glossary.md)。

## 定位与职责

定义 crawler 与 process 共享的业务数据语义：哪些数据必须原样保留、哪些字段表达数据事实、执行进度放在哪里、写入与消费如何避免重复和丢数。物理列名与约束不在此重复，以 DDL 为准。

## 结构与约定

### 模型

适配器注册 + 分类 → 渠道 → 资源 → 内容：四张业务表 + 适配器注册表。

| 表 | 职责 |
|---|---|
| `crawler_adapter` | 适配器注册：抓取适配器目录，parser 隐式绑定 |
| `channel_category` | 渠道分类：树形归属（站点 → 模块 → 栏目） |
| `channel` | 数据渠道：一个可拉取的数据供给单元 |
| `data_source` | 资源登记：每个要抓的资源连接一行 |
| `data_detail` | 资源内容：资源产出的原始数据，source : detail = 1 : N；行存在 = 详情已抓取 |

### 数据与执行状态分离

- 业务行只存数据语义；执行进度以 `exec_status` 显式表达，重试细节在 `extra` 约定键；执行引擎状态可重建、可重放；
- `data_status` 记数据事实，`exec_status` 记执行进度，两列独立；执行细节在 `extra` 约定键（`fail_count` / `next_retry_at` / `last_error`；新增约定键须先与项目所有者确认）；
- 采集库只存原始数据：解析仅限抓取必需（record_key 预映射、详情抓取目标现场解析），标题/业务时间等一切解析产物归分析侧；
- 治理按 `create_time` 范围分批归档 + 保留期声明（`channel.retention_days`）；首期不分区。

### record_key

`record_key` 格式「渠道前缀id:sha256」的精确规则【待确认】：前缀与 id 来源、hash 输入（source = 规范化链接；detail = 内容键 + 时间窗）、时间窗格式与时区；预映射与哈希由代码生成、PG 只存。

- source 键 = 资源唯一性（链接不变行不变）；
- detail 键 = 内容唯一性（可含时间窗）；
- `data_source` 不设独立 URL 列：资源定位与详情抓取目标由 raw + parser 现场解析，record_key 负责资源去重。

### 原始载荷

- raw（bytea）为未解析原始载荷，双侧留存（source 条目 / detail 全量）；
- 在 `channel.retention_days` 声明的保留期内必须可达，超出保留期后可归档或清理。

### 写入规则

写入模式是渠道配置，按 source/detail 行级快照：

| 模式 | 行为 | 约束 |
|---|---|---|
| overwrite | 覆盖（upsert） | 幂等；重复执行不产生重复行 |
| skip | 不处理（DO NOTHING） | 已存在行保持不变 |
| append | 增加（直接插入） | 要求 record_key 含时间窗 |

detail 写入使用 source 行快照还是当前 channel 配置【待确认】。

### 水位与时间

- 分发采用双键水位：消费方按 `(update_time, id)` 升序扫描；新行和更新行都通过 `update_time` 进入水位，id 用于同一时间戳内的稳定排序；
- `update_time` 由写入方显式刷新；基线 DDL 不建触发器，overwrite / append / 状态更新都不得依赖 `DEFAULT now()` 代替更新。

### 适配器与枚举

- adapter 列语义：`adapter` = 产出本行的适配器编码快照；`detail_adapter` = 详情抓取适配器编码；两者都存编码，不建外键；
- parser 隐式绑定 adapter（驱动抓取的最小解析），不单独配置；
- 适配器目录由 `crawler_adapter` 表登记，业务表存编码，不建外键；
- 枚举列为 text：稳定集合加 CHECK 约束，开放集合（适配器编码）由代码注册表校验。

### 表级约定

- 每表三件套 `id` / `create_time` / `update_time`；表列必须 `COMMENT ON`；
- `extra` 为表级扩展容器，固定列不够时启用；
- `data_detail.channel_id` 必须等于所属 `data_source.channel_id`；数据库不强制，由 repository 写入时保证；
- `data_detail` 行存在 = 详情已抓取；失败尝试只记在 `data_source.exec_status` / `extra`。若未来允许失败 detail 行，须先修改本契约与 DDL CHECK。

## 核心流程

### 列表写入

crawler 拉取列表 → parser 生成 record_key 预映射与详情目标 → repository 按渠道 write_mode 写入 `data_source`。overwrite 幂等、skip 保留旧行、append 依赖含时间窗的 record_key。

### 详情回填

按 `data_source.exec_status` 取待回填行 → detail adapter 抓取 → repository 按配置写入 `data_detail` → 行存在即详情已抓取，`exec_status` 落成功。失败尝试只更新执行状态与 extra，不生成失败 detail 行。

### 水位消费

process（以及任何按增量消费的读取方）按 `(update_time, id)` 升序拉取；写入方每次更新都必须刷新 `update_time`，否则消费方会漏行。双键水位的消费侧归属见 [crawler.md](crawler.md) 与 [process.md](process.md)。

### 保留与归档

按 `create_time` 范围分批归档，保留期由 `channel.retention_days` 声明；超出保留期后才可清理原始载荷。

## 数据与接口

- source : detail = 1 : N；detail 只能挂在存在的 source 下；
- `data_detail.channel_id` 与所属 source 一致，由 repository 保证；
- 适配器编码、parser 编码、枚举取值与 DDL 的 CHECK / COMMENT 对齐；
- 业务表是 crawler 与 process 之间的数据契约；process 不得依赖 crawler 的内部实体或实现；
- 物理结构变更必须先改 DDL 与本文，再改代码；DDL 是幂等基线，不提供自动向下迁移。

## 配置

- `channel.retention_days`：原始载荷保留期；
- `channel.schedule`：调度参数，JSON 键、类型与时区尚未定稿，见 [admin.md](admin.md) 与 [distributed.md](distributed.md)；
- `channel.extra`：渠道实例参数与适配器参数，键表见 [crawler.md](crawler.md)；
- 写入模式是渠道配置，source/detail 行级快照；detail 快照来源待确认。

## 测试与守门

- DDL 必须幂等，可在干净库重复执行；
- repository 的 write_mode、双键水位、`update_time` 刷新、source/detail 一致性必须有测试；
- 表列 COMMENT、CHECK、索引与 DDL 基线必须有契约测试；
- 文档与 DDL 的引用由 verify 的文档四检查拦截，判据见 [docs/README.md](../README.md)。

## 待确认事项

| 事项 | 说明 |
|---|---|
| `record_key` 精确格式 | 前缀与 id 来源、hash 输入、时间窗格式与时区 |
| detail 写入快照来源 | 使用 source 行快照还是当前 channel 配置 |
| `data_status` 是否跳过详情回填 | filtered / dead / paywalled 的处理 |
| `data_detail` 失败行语义 | 当前约定失败不生成 detail 行；未来是否允许失败行需改契约与 DDL |
| 双键水位消费侧 | process 消费方式与水位表设计 |

## 相关文档

- [system-overview.md](system-overview.md)、[distributed.md](distributed.md)、[auth.md](auth.md)：模块边界与相邻契约；
- [crawler.md](crawler.md)、[process.md](process.md)：写入方与消费方；
- [001_business_tables.sql](../../db/001_business_tables.sql)：物理结构唯一权威；
- [glossary.md](../00-overview/glossary.md)：术语统一。
