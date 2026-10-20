# 数据模型（物理表）

> 平台唯一物理表设计，受 AGENTS.md 数据模型契约约束；本文 DDL 为 `db/` migration 基线蓝本。

## 一、模型：分类 → 渠道 → 资源 → 内容

| 表 | 职责 |
|---|---|
| channel_category | 渠道分类：树形归属（站点→模块→栏目），path 为人读唯一标识 |
| channel | 数据渠道：一个可拉取的数据供给单元（= 一条采集管线），配置适配器 / 解析器 / 节奏 / 监控 / 保留期 |
| data_source | 资源登记：每个要抓的资源连接一行（链接不变行不变），raw 载荷 + 解析投影 |
| data_detail | 资源内容：资源抓取产出的原始内容，**source : detail = 1 : N** |

**关键规则**（实现必须遵守，细节见 DDL 注释）：

- **两类唯一键，生成是纯代码逻辑**：source 键 = 资源唯一性（同一链接同一行）；detail 键 = 内容唯一性（适配器按业务要求生成、可含时间窗，榜单按天/快讯按条）。格式统一「渠道前缀id:sha256(预映射)」，PG 只存不参与计算；
- **raw 为真相源**：data_source / data_detail 各存未解析原始载荷（bytea 任意格式），title/event_time/extra 为解析投影；重放 = 按行内 adapter/parser 快照对 raw 重跑解析；详情抓取目标也由适配器对 source.raw 现场解析（不占列），实际地址固化在 data_detail.fetch_url；
- **覆盖式写入唯一**：ON CONFLICT DO UPDATE + 哈希条件，两层生效——source 按 `(channel_id, record_key)`、detail 按 `(source_id, record_key)`：

  ```sql
  ... ON CONFLICT (channel_id, record_key) DO UPDATE SET
          title = EXCLUDED.title, extra = EXCLUDED.extra,
          content_hash = EXCLUDED.content_hash,
          exec_status = 'pending',            -- 载荷被源站修订 → 详情过期，重新排队回填
          update_time = now()
      WHERE data_source.content_hash IS DISTINCT FROM EXCLUDED.content_hash;
  -- 内容未变不推进 update_time（同步水位不空转）；detail 侧同型（content_hash 为 raw 的哈希，
  -- 冲突覆盖时不回退 exec_status）
  ```

  "要最新态"还是"要历史"由 detail 键预映射表达：稳定 id → 同键覆盖；条目标识+时间窗 → 每窗新行，历史天然保留；
- **双状态列**（data_source / data_detail 同款，2026-10-20 拆分拍板）：
  - `data_status` = 数据事实（终态结论）：normal 正常 / filtered 规则跳过 / dead 源站已删（不再重试）/ paywalled 付费墙（不突破）；
  - `exec_status` = 执行进度（采集生命周期）：pending 待运行 / running 运行中 / failed 失败（重试中）/ success 成功（处理闭环）——success 配合 data_status 区分具体结论（抓到 / 跳过 / 已删 / 付费墙）；
  - 执行细节仍走 extra 约定键（fail_count / next_retry_at / last_error，已确认）：failed 只说明"在重试"，次数/时间/原因在 extra；
  - 引擎行为：回填取件 = `exec_status='pending' AND data_status='normal'`；running 由引擎抓取前写入、启动清扫僵尸 running→pending（进程崩溃残留）；source 载荷修订（哈希变化）时 exec_status 重置 pending 重新回填；INLINE（列表即全文）source 与 detail 同轮直出 success；
- **枚举单一代码真相源，不设数据库镜像表**（2026-10-20 拍板，弃 dict）：
  - 稳定状态机（各表 status）→ `text + CHECK`，加值 = 事务内改约束（毫秒级）+ 代码枚举加值；
  - 开放集合（adapter / parser）→ `text` 存稳定编码（如 `'em.news'`），代码注册表是唯一真相源，运行时 fail-fast（渠道加载遇未知编码即抛）；新增适配器 = 只写代码 + 渠道行，无同步动作；
  - 不用 PG 原生 ENUM（加值要 ALTER TYPE 且不能删值）；文本编码跨环境稳定，裸 SQL 直读即懂；控制台展示名由共享代码枚举渲染；
- **extra 是表级扩展容器**：固定列不够时启用；新增约定键须先与项目所有者确认（已确认约定键：fail_count/next_retry_at/last_error）；
- **每表三件套**：`id`（bigint identity）+ `create_time` + `update_time`（无触发器，写入收敛到统一出口，SQL 显式 `SET update_time = now()`）；表列必须 COMMENT ON（写"是什么"，行内注释写"为什么"）；
- **治理**：retention_days 保留期声明；首期不分区（event_time 索引），千万行级再演 RANGE 分区；
- **adapter/parser 命名**：无前缀 = 本表自己的快照，带下游前缀（如 detail_adapter）= 下游阶段的配置或计划；
- **channel 的克制边界**：只承载分类归属 + 适配器配置 + 实例参数（extra）+ 节奏监控；不加 host/site、dataset 分组、统计缓存等可推导列。

## 二、DDL

```sql
-- ============================================================
-- 表 1：channel_category 渠道分类表（树形归属：站点 → 模块 → 栏目）
-- ============================================================
CREATE TABLE channel_category (
    id          bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    parent_id   bigint REFERENCES channel_category(id),   -- NULL = 根（站点级）；层级深度由链表达
    name        text NOT NULL,                            -- 分类名：财联社 / 电报快讯 / 要闻
    path        text NOT NULL UNIQUE,                     -- 物化路径 'cls/flash'：人读 + 子树查询
    sort_no     int NOT NULL DEFAULT 0,                   -- 同层排序（控制台展示）
    status      text NOT NULL DEFAULT 'active'
        CHECK (status IN ('active', 'disabled')),         -- 启停（稳定状态机：text + CHECK）
    create_time timestamptz NOT NULL DEFAULT now(),
    update_time timestamptz NOT NULL DEFAULT now()
);
COMMENT ON TABLE channel_category IS '渠道分类表：树形归属（站点→模块→栏目），人读层级的唯一归属地';
COMMENT ON COLUMN channel_category.id IS '代理主键';
COMMENT ON COLUMN channel_category.parent_id IS '父分类，NULL=根（站点级）';
COMMENT ON COLUMN channel_category.name IS '分类名';
COMMENT ON COLUMN channel_category.path IS '物化路径（如 cls/flash）：人读 + 子树查询，同层不重复';
COMMENT ON COLUMN channel_category.sort_no IS '同层排序';
COMMENT ON COLUMN channel_category.status IS '启停：active 启用/disabled 停用；新增取值须同步本注释';
COMMENT ON COLUMN channel_category.create_time IS '创建时间';
COMMENT ON COLUMN channel_category.update_time IS '修改时间';
-- 删除保护：有 channel 挂载或有子节点的分类禁止删除（应用层保证，不建约束）

-- ============================================================
-- 表 2：channel 数据渠道（配置表，平台能力的挂载点）
-- ============================================================
CREATE TABLE channel (
    id             bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    -- ── 身份 ──
    category_id    bigint NOT NULL REFERENCES channel_category(id),
                                           -- 归属分类（树形：站点→模块→栏目，通常挂叶子）；
                                           --   内容域由站点级归属与适配器推导，不设独立列
    channel_name   text NOT NULL,
    description    text,                  -- 人读说明：站点侦察结论 / 已知坑的就近落点
    -- ── 接入 ──
    source_adapter text NOT NULL,
                                           -- source 列表获取适配器编码（如 'em.news'）：
                                           --   代码注册表按编码解析到实现类；抓取方式与载荷形式
                                           --   全部固化在适配器实现里；未知编码加载即抛（fail-fast）
    source_parser  text NOT NULL,
                                           -- source 列表解析器编码：代码注册表解析，纯函数可离线回归
    -- ── 治理 ──
    retention_days int CHECK (retention_days IS NULL OR retention_days > 0),
                                           -- 保留期声明，NULL = 永久（契约第 5 条）
    -- ── 调度与监控 ──
    schedule       jsonb NOT NULL DEFAULT '{}',   -- 拉取节奏：间隔/cron + 并发互斥声明
    monitor        jsonb NOT NULL DEFAULT '{}',   -- 监控阈值：新鲜度告警/可用性判定窗口
    last_success_at timestamptz,                  -- 最近一次成功拉取（监控回写缓存，可从数据重算）
    -- ── 状态与兜底 ──
    status         text NOT NULL DEFAULT 'active'
        CHECK (status IN ('active', 'paused', 'retired')),
    extra          jsonb NOT NULL DEFAULT '{}',
        -- 渠道级实例参数（适配器的差异化输入）：栏目号/频道参数、拉取窗口、停机回补上限、
        -- 每轮批量上限、凭据引用名（credential_key 键，只存引用不存值，真实凭据环境注入）等
    create_time    timestamptz NOT NULL DEFAULT now(),
    update_time    timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT channel_uk UNIQUE (category_id, channel_name)   -- 同分类下渠道名唯一
);
COMMENT ON TABLE channel IS '数据渠道表：一个可拉取的数据供给单元（挂在分类节点下），平台能力的挂载点';
COMMENT ON COLUMN channel.id IS '代理主键';
COMMENT ON COLUMN channel.category_id IS '归属分类（channel_category.id），通常挂叶子';
COMMENT ON COLUMN channel.channel_name IS '渠道名（同分类下唯一）';
COMMENT ON COLUMN channel.description IS '人读说明：站点侦察结论/已知坑';
COMMENT ON COLUMN channel.source_adapter IS 'source 列表获取适配器编码（如 em.news）：代码注册表解析到实现类，抓取方式固化在适配器里';
COMMENT ON COLUMN channel.source_parser IS 'source 列表解析器编码：代码注册表解析';
COMMENT ON COLUMN channel.retention_days IS '保留期（天），NULL=永久';
COMMENT ON COLUMN channel.schedule IS '拉取节奏：间隔/cron + 并发互斥声明';
COMMENT ON COLUMN channel.monitor IS '监控阈值：新鲜度告警/可用性判定窗口';
COMMENT ON COLUMN channel.last_success_at IS '最近一次成功拉取（监控回写缓存，可从数据重算）';
COMMENT ON COLUMN channel.status IS '渠道状态：active 启用中/paused 已暂停/retired 已退役；新增取值须同步本注释';
COMMENT ON COLUMN channel.extra IS '渠道级实例参数：栏目号/窗口/批量上限/凭据引用名等';
COMMENT ON COLUMN channel.create_time IS '创建时间';
COMMENT ON COLUMN channel.update_time IS '修改时间';

-- ============================================================
-- 表 3：data_source 数据源列表（资源登记：每个要抓的资源连接一行）
-- ============================================================
CREATE TABLE data_source (
    id            bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    channel_id    bigint NOT NULL REFERENCES channel(id),   -- 渠道数字外键
    record_key    text NOT NULL,          -- 资源唯一键，格式「渠道前缀id:sha256」；
                                          --   预映射 = 适配器按资源唯一性要求从载荷解析的唯一标识，
                                          --   同一链接同一行；哈希由应用层计算、PG 只存储（代码逻辑）；
                                          --   生成规则适配器声明、选定后不可换（去重键稳定性，契约第 2 条）
    title         text,                   -- 解析投影：标题（最热字段实物列，其余差异化字段进 extra）
    event_time    timestamptz,            -- 解析投影：业务时间（源站发布时间），与 create_time 分离（契约第 4 条）
    adapter       text NOT NULL,
        -- 列表获取适配器编码快照：实际产出本行的实现类（溯源：谁拉到了这条数据）
    parser        text NOT NULL,
        -- 列表解析器编码快照：实际解析出本行的解析器
    raw           bytea NOT NULL,         -- 原始载荷：列表响应中本条目的未解析原文（JSON 片段/HTML 分段）；
                                          --   title/event_time/extra 均由 parser 对本列解析产出；
                                          --   解析重放基于本列
    detail_adapter text NOT NULL,
        -- 详情获取适配器编码：本条数据的详情怎么抓的实现类；
        --   「列表即全文」场景与自身同源
    detail_parser text NOT NULL,
        -- 详情解析器编码：本条详情的解析器
    data_status   text NOT NULL DEFAULT 'normal'
        CHECK (data_status IN ('normal', 'filtered', 'dead', 'paywalled')),
        -- 数据事实（终态结论）：正常 / 规则跳过 / 源站已删（不再重试）/ 付费墙（不突破）
    exec_status   text NOT NULL DEFAULT 'pending'
        CHECK (exec_status IN ('pending', 'running', 'failed', 'success')),
        -- 执行进度（采集生命周期）：待运行 / 运行中 / 失败（重试中，细节在 extra 约定键）/
        --   成功（处理闭环，配合 data_status 区分抓到/跳过/已删/付费墙）；
        --   回填取件 = pending + data_status=normal；载荷修订时引擎重置 pending
    extra         jsonb NOT NULL DEFAULT '{}',
        -- 渠道差异化字段：summary / author / 栏目 / 机构 / 文号 / 是否付费 / 榜单数值 ...；
        --   执行态约定键（fail_count/next_retry_at/last_error，已确认）也在此；
        --   新增约定键须先与项目所有者确认
    content_hash  text,                   -- 载荷哈希（raw 的哈希）：资源元数据/载荷被源站修订时才有变化
    create_time   timestamptz NOT NULL DEFAULT now(),
    update_time   timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT data_source_uk UNIQUE (channel_id, record_key)   -- 契约第 2 条：去重键
);
COMMENT ON TABLE data_source IS '数据源列表表：资源登记（每个要抓的资源连接一行，元数据为载荷解析投影）';
COMMENT ON COLUMN data_source.id IS '代理主键';
COMMENT ON COLUMN data_source.channel_id IS '归属渠道（channel.id）';
COMMENT ON COLUMN data_source.record_key IS '资源唯一键：适配器按资源唯一性要求从载荷解析的唯一标识（文章id/doc_id/url唯一段等）；哈希应用层计算、PG只存储；选定不可换';
COMMENT ON COLUMN data_source.title IS '解析投影：标题（最热字段实物列）';
COMMENT ON COLUMN data_source.event_time IS '解析投影：业务时间（源站发布时间），与 create_time 分离';
COMMENT ON COLUMN data_source.adapter IS '列表获取适配器编码快照：实际产出本行的实现类编码';
COMMENT ON COLUMN data_source.parser IS '列表解析器编码快照：实际解析出本行的实现类编码';
COMMENT ON COLUMN data_source.raw IS '原始载荷：列表响应中本条目的未解析原文，各投影列由 parser 对本列解析产出；重放依据';
COMMENT ON COLUMN data_source.detail_adapter IS '详情获取适配器编码：本条数据详情获取的实现类；列表即全文场景与自身同源';
COMMENT ON COLUMN data_source.detail_parser IS '详情解析器编码：本条详情的解析器';
COMMENT ON COLUMN data_source.data_status IS '数据事实状态：normal 正常/filtered 规则跳过/dead 源站已删（不再重试）/paywalled 付费墙；新增取值须同步本注释';
COMMENT ON COLUMN data_source.exec_status IS '执行状态：pending 待运行/running 运行中/failed 失败重试中/success 成功（处理闭环）；重试细节在 extra 约定键';
COMMENT ON COLUMN data_source.extra IS '渠道差异化扩展字段 + 执行态约定键（新增约定键须先与项目所有者确认）';
COMMENT ON COLUMN data_source.content_hash IS '载荷哈希（raw 的哈希）：覆盖式写入的跳过更新依据';
COMMENT ON COLUMN data_source.create_time IS '创建时间（首次入库）';
COMMENT ON COLUMN data_source.update_time IS '修改时间（同步水位键之一）';

CREATE INDEX data_source_time_idx  ON data_source (event_time DESC);   -- 新鲜度/归档剪枝
CREATE INDEX data_source_sync_idx ON data_source (update_time, id);   -- 分发双水位

-- ============================================================
-- 表 4：data_detail 数据详情（资源抓取产出的原始内容，source : detail = 1 : N）
-- ============================================================
CREATE TABLE data_detail (
    id            bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    channel_id    bigint NOT NULL,        -- 冗余渠道（与 source 一致，便于按渠道查询/归档）
    source_id     bigint NOT NULL REFERENCES data_source(id),
                                          -- 来源资源（一个资源可产出多份内容）
    record_key    text NOT NULL,          -- 内容唯一键，格式「渠道前缀id:sha256」；
                                          --   预映射 = 适配器按内容唯一性要求生成、可含时间窗
                                          --   （榜单按天、快讯按条），同一资源可产出多份内容；
                                          --   哈希由应用层计算、PG 只存储不参与计算（代码逻辑）
    fetch_url     text NOT NULL,          -- 抓取态快照：实际请求的地址（可能经重定向）
    adapter       text NOT NULL,
        -- 详情获取适配器编码快照：实际抓取本详情的实现类（重放依据）
    parser        text NOT NULL,
        -- 详情解析器编码快照：实际解析出本详情的实现类
    raw           bytea NOT NULL,         -- 原始数据：未解析的原始载荷，格式不限（文本/二进制/图片/
                                          --   复合或自定义结构），格式定义与解码在 adapter、PG 只存字节；
                                          --   解析重放基于本列，解析产物归分析侧
    content_hash  text,                   -- 载荷哈希（raw 的哈希）：同键重抓哈希变才覆盖
    data_status   text NOT NULL DEFAULT 'normal'
        CHECK (data_status IN ('normal', 'filtered', 'dead', 'paywalled')),
        -- 数据事实（语义同 data_source.data_status）
    exec_status   text NOT NULL DEFAULT 'success'
        CHECK (exec_status IN ('pending', 'running', 'failed', 'success')),
        -- 执行进度：行由成功抓取产出，落行即 success；
        --   pending/running/failed 预留后续阶段（重抓修订、回写）使用
    extra         jsonb NOT NULL DEFAULT '{}',
        -- 扩展字段：附件链接等差异化信息 + 执行态约定键（新增约定键须先与项目所有者确认）
    event_time    timestamptz,            -- 详情页修订后的准确业务时间（源站事实）
    create_time   timestamptz NOT NULL DEFAULT now(),
    update_time   timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT data_detail_uk UNIQUE (source_id, record_key),   -- 同一资源下内容键唯一
    CONSTRAINT data_detail_fk FOREIGN KEY (source_id)
        REFERENCES data_source (id) ON DELETE CASCADE
);
COMMENT ON TABLE data_detail IS '数据详情表：资源抓取产出的原始内容（source:detail=1:N，未解析原始载荷，行存在=详情已抓取）';
COMMENT ON COLUMN data_detail.id IS '代理主键';
COMMENT ON COLUMN data_detail.channel_id IS '归属渠道（冗余，与 source 一致）';
COMMENT ON COLUMN data_detail.source_id IS '来源资源（data_source.id），同一资源可产出多份内容';
COMMENT ON COLUMN data_detail.record_key IS '内容唯一键：适配器按内容唯一性要求生成、可含时间窗；哈希应用层计算、PG只存储；选定不可换';
COMMENT ON COLUMN data_detail.fetch_url IS '详情抓取地址（抓取态快照，溯源/重放）';
COMMENT ON COLUMN data_detail.adapter IS '详情获取适配器编码快照：实际抓取本详情的实现类编码';
COMMENT ON COLUMN data_detail.parser IS '详情解析器编码快照：实际解析出本详情的实现类编码';
COMMENT ON COLUMN data_detail.raw IS '原始数据：未解析原始载荷（bytea 任意格式：文本/二进制/图片/自定义结构），解码在 adapter；重放依据';
COMMENT ON COLUMN data_detail.content_hash IS '载荷哈希（raw 的哈希）：同键重抓哈希变才覆盖';
COMMENT ON COLUMN data_detail.data_status IS '数据事实状态：normal 正常/filtered 规则跳过/dead 源站已删/paywalled 付费墙；新增取值须同步本注释';
COMMENT ON COLUMN data_detail.exec_status IS '执行状态：行由成功抓取产出，落行即 success；pending/running/failed 预留后续阶段';
COMMENT ON COLUMN data_detail.extra IS '扩展字段：附件链接等差异化信息 + 执行态约定键（新增约定键须先与项目所有者确认）';
COMMENT ON COLUMN data_detail.event_time IS '详情页修订后的准确业务时间';
COMMENT ON COLUMN data_detail.create_time IS '创建时间（首次入库）';
COMMENT ON COLUMN data_detail.update_time IS '修改时间（同步水位键之一）';

CREATE INDEX data_detail_time_idx  ON data_detail (event_time DESC);
CREATE INDEX data_detail_sync_idx ON data_detail (update_time, id);
```

## 三、v1 七管线装填对照

| v1 管线 | 资源（data_source） | 内容（data_detail） |
|---|---|---|
| 网页新闻 / 研报 / 官方文件 | 每篇 / 每文件一资源 | 1:1（key=常量），正文原文进 raw |
| 实时快讯（五源） | 列表接口一资源 | 1:N（key=条目id），同源直出 |
| 盘面榜单（龙虎榜/大宗/两融） | 每榜单接口一资源（链接恒定） | 1:N（key 含交易日） |
| 公告（巨潮） | 每公告一资源 | 1:1，PDF 原样进 raw |
| 数据集（loot-drop） | 对账资源一行 | 1:N（key=slug），哈希修订 |
