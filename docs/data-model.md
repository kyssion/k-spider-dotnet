# 数据模型（物理表）

> 平台唯一物理表设计；本文 DDL 为 `db/` migration 基线蓝本。

## 一、模型

分类 → 渠道 → 资源 → 内容，四张业务表 + 节点注册设施表：

| 表 | 职责 |
|---|---|
| crawler_adapter | 适配器注册：抓取适配器目录（admin 聚合节点上报维护），parser 隐式绑定 |
| channel_category | 渠道分类：树形归属（站点→模块→栏目） |
| channel | 数据渠道：一个可拉取的数据供给单元 |
| data_source | 资源登记：每个要抓的资源连接一行 |
| data_detail | 资源内容：资源产出的原始数据，source : detail = 1 : N |
| sys_node | 节点注册：全部服务节点的心跳与状态登记（分布式设施表） |

规则：

- 采集库只存原始数据：解析仅限抓取必需（record_key 预映射、详情抓取目标现场解析），标题/业务时间等一切解析产物归分析侧；
- record_key 格式「渠道前缀id:sha256」：预映射与哈希由代码生成、PG 只存；source 键 = 资源唯一性（链接不变行不变），detail 键 = 内容唯一性（可含时间窗）；
- raw（bytea）为未解析原始载荷，双侧留存（source 条目 / detail 全量）；
- 写入规则三模式（渠道配置，source/detail 行级快照）：overwrite 覆盖（upsert）/ skip 不处理（DO NOTHING）/ append 增加（直接插入，要求 record_key 含时间窗）；
- data_status 记数据事实，exec_status 记执行进度，两列独立；执行细节在 extra 约定键（fail_count / next_retry_at / last_error；新增约定键须先与项目所有者确认）；
- adapter 命名：无前缀 = 本表自己的快照，带下游前缀（detail_）= 下游计划；parser 隐式绑定 adapter（驱动抓取的最小解析），不单独配置；
- 适配器目录：crawler_adapter 表登记（admin 聚合节点上报维护），业务表存编码，不建外键（节点未上报时不应阻塞渠道创建）；
- 枚举列为 text：稳定集合加 CHECK 约束，开放集合（适配器编码）由代码注册表校验；
- 每表三件套 id / create_time / update_time；表列必须 COMMENT ON；
- extra 为表级扩展容器，固定列不够时启用。

## 二、DDL

```sql
-- ============================================================
-- crawler_adapter 适配器注册表（admin 聚合节点上报维护）
-- ============================================================
CREATE TABLE crawler_adapter (
    id           bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code         text NOT NULL UNIQUE,    -- 适配器编码（如 'em.news' / 'em.news_detail'），与代码注册表对齐
    type         text NOT NULL
        CHECK (type IN ('source', 'detail')),
                                           -- source = 渠道列表抓取；detail = 资源详情抓取
    name         text NOT NULL,            -- 展示名
    description  text,                     -- 描述：抓什么、怎么抓的要点
    parser_code  text NOT NULL,
                                           -- 隐式绑定的解析器编码：驱动抓取的最小解析（record_key 预映射、
                                           --   详情抓取目标）；渠道配置无需感知，此处仅登记供目录展示与溯源
    create_time  timestamptz NOT NULL DEFAULT now(),
    update_time  timestamptz NOT NULL DEFAULT now()
);
COMMENT ON TABLE crawler_adapter IS '适配器注册表：抓取适配器目录（admin 聚合节点上报维护），parser 隐式绑定';
COMMENT ON COLUMN crawler_adapter.id IS '代理主键';
COMMENT ON COLUMN crawler_adapter.code IS '适配器编码（如 em.news），与代码注册表对齐，唯一';
COMMENT ON COLUMN crawler_adapter.type IS '适配器类型：source 渠道列表抓取 / detail 资源详情抓取';
COMMENT ON COLUMN crawler_adapter.name IS '展示名';
COMMENT ON COLUMN crawler_adapter.description IS '描述：抓什么、怎么抓的要点';
COMMENT ON COLUMN crawler_adapter.parser_code IS '隐式绑定的解析器编码：驱动抓取的最小解析；渠道无需配置，仅登记供目录展示与溯源';
COMMENT ON COLUMN crawler_adapter.create_time IS '创建时间';
COMMENT ON COLUMN crawler_adapter.update_time IS '修改时间';

-- ============================================================
-- channel_category 渠道分类表（树形归属：站点 → 模块 → 栏目）
-- ============================================================
CREATE TABLE channel_category (
    id          bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    parent_id   bigint REFERENCES channel_category(id),   -- NULL = 根（站点级）；层级深度由链表达
    name        text NOT NULL,                            -- 分类名：财联社 / 电报快讯 / 要闻
    path        text NOT NULL UNIQUE,                     -- 物化路径 'cls/flash'：人读 + 子树查询
    sort_no     int NOT NULL DEFAULT 0,                   -- 同层排序（控制台展示）
    status      text NOT NULL DEFAULT 'active'
        CHECK (status IN ('active', 'disabled')),
    create_time timestamptz NOT NULL DEFAULT now(),
    update_time timestamptz NOT NULL DEFAULT now()
);
COMMENT ON TABLE channel_category IS '渠道分类表：树形归属（站点→模块→栏目）';
COMMENT ON COLUMN channel_category.id IS '代理主键';
COMMENT ON COLUMN channel_category.parent_id IS '父分类，NULL=根（站点级）';
COMMENT ON COLUMN channel_category.name IS '分类名';
COMMENT ON COLUMN channel_category.path IS '物化路径（如 cls/flash）：人读 + 子树查询，同层不重复';
COMMENT ON COLUMN channel_category.sort_no IS '同层排序';
COMMENT ON COLUMN channel_category.status IS '启停：active 启用/disabled 停用；新增取值须同步本注释';
COMMENT ON COLUMN channel_category.create_time IS '创建时间';
COMMENT ON COLUMN channel_category.update_time IS '修改时间';

-- ============================================================
-- channel 数据渠道表（配置表，平台能力的挂载点）
-- ============================================================
CREATE TABLE channel (
    id             bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    -- ── 身份 ──
    category_id    bigint NOT NULL REFERENCES channel_category(id),
                                           -- 归属分类（树形：站点→模块→栏目，通常挂叶子）；
                                           --   内容域由站点级归属与适配器推导，不设独立列
    channel_name   text NOT NULL,
    -- ── 接入 ──
    source_adapter text NOT NULL,
                                           -- source 列表获取适配器编码（如 'em.news'）：
                                           --   代码注册表按编码解析到实现类；抓取方式（策略/URL模板/
                                           --   请求签名/反爬放行集）与解析全部固化在适配器实现里
                                           --   （parser 隐式绑定），未知编码加载即抛（fail-fast）
    detail_adapter text NOT NULL,
                                           -- 详情适配器编码：本渠道数据的详情怎么抓；
                                           --   「列表即全文」场景与 source_adapter 同值
    -- ── 治理 ──
    write_mode    text NOT NULL DEFAULT 'overwrite'
        CHECK (write_mode IN ('overwrite', 'skip', 'append')),
                                           -- 入库规则：overwrite 覆盖（upsert）/ skip 不处理（DO NOTHING）/
                                           --   append 增加（直接插入，要求 record_key 含时间窗）
    retention_days int CHECK (retention_days IS NULL OR retention_days > 0),
                                           -- 保留期声明，NULL = 永久（契约第 5 条）
    schedule       jsonb NOT NULL DEFAULT '{}',   -- 拉取节奏：间隔/cron + 并发互斥声明
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
COMMENT ON TABLE channel IS '数据渠道表：一个可拉取的数据供给单元';
COMMENT ON COLUMN channel.id IS '代理主键';
COMMENT ON COLUMN channel.category_id IS '归属分类（channel_category.id），通常挂叶子；内容域由站点归属与适配器推导，不设独立列';
COMMENT ON COLUMN channel.channel_name IS '渠道名（同分类下唯一）';
COMMENT ON COLUMN channel.source_adapter IS '列表适配器编码（如 em.news），代码注册表解析到实现类；抓取方式与解析固化在适配器里（parser 隐式绑定）';
COMMENT ON COLUMN channel.detail_adapter IS '详情适配器编码：本渠道数据的详情怎么抓；列表即全文场景与 source_adapter 同值';
COMMENT ON COLUMN channel.write_mode IS '入库规则：overwrite 覆盖/skip 不处理/append 增加（要求 record_key 含时间窗）；source/detail 行快照来源';
COMMENT ON COLUMN channel.retention_days IS '保留期（天），NULL=永久';
COMMENT ON COLUMN channel.schedule IS '拉取节奏：间隔/cron 等';
COMMENT ON COLUMN channel.status IS '渠道状态：active 启用/paused 已暂停/retired 已退役；新增取值须同步本注释';
COMMENT ON COLUMN channel.extra IS '渠道实例参数：栏目号/拉取窗口/批量上限/凭据引用名（credential_key 键，只存引用不存值，真实凭据环境注入）等';
COMMENT ON COLUMN channel.create_time IS '创建时间';
COMMENT ON COLUMN channel.update_time IS '修改时间';

-- ============================================================
-- data_source 数据源列表表（资源登记：每个要抓的资源连接一行）
-- ============================================================
CREATE TABLE data_source (
    id            bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    channel_id    bigint NOT NULL REFERENCES channel(id),   -- 渠道数字外键
    record_key    text NOT NULL,          -- 资源唯一键，格式「渠道前缀id:sha256」；
                                          --   预映射 = 适配器按资源唯一性要求从载荷解析的唯一标识
                                          --   （文章id/doc_id/url 唯一段等），同一链接同一行；
                                          --   哈希由应用层计算、PG 只存储不参与计算（代码逻辑）；
                                          --   生成规则适配器声明、选定后不可换（去重键稳定性，契约第 2 条）
    adapter       text NOT NULL,
        -- 列表适配器编码快照：实际产出本行的实现类（溯源：谁拉到了这条数据）
    raw           bytea NOT NULL,         -- 原始载荷：列表响应中本条目的未解析原文（JSON 片段/HTML 分段）；
                                          --   record_key 预映射与详情抓取目标从本列解析；
                                          --   解析重放基于本列
    detail_adapter text NOT NULL,
        -- 详情适配器编码：本条数据的详情怎么抓的实现类；
        --   「列表即全文」场景与自身同源
    write_mode    text NOT NULL DEFAULT 'overwrite'
        CHECK (write_mode IN ('overwrite', 'skip', 'append')),
        -- 入库规则快照（继承渠道配置）：本行的冲突处理方式
    data_status   text NOT NULL DEFAULT 'normal'
        CHECK (data_status IN ('normal', 'filtered', 'dead', 'paywalled')),
        -- 数据事实（终态结论）：正常 / 规则跳过 / 源站已删 / 付费墙
    exec_status   text NOT NULL DEFAULT 'pending'
        CHECK (exec_status IN ('pending', 'running', 'failed', 'success')),
        -- 执行进度（采集生命周期）：待运行 / 运行中 / 失败（重试细节在 extra 约定键）/ 成功
    extra         jsonb NOT NULL DEFAULT '{}',
        -- 执行态约定键（fail_count/next_retry_at/last_error）；
        --   新增约定键须先与项目所有者确认
    create_time   timestamptz NOT NULL DEFAULT now(),
    update_time   timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT data_source_uk UNIQUE (channel_id, record_key)   -- 契约第 2 条：去重键
);
COMMENT ON TABLE data_source IS '数据源列表表：资源登记，每个要抓的资源连接一行';
COMMENT ON COLUMN data_source.id IS '代理主键';
COMMENT ON COLUMN data_source.channel_id IS '归属渠道（channel.id）';
COMMENT ON COLUMN data_source.record_key IS '资源唯一键「渠道前缀id:sha256」：预映射=适配器按资源唯一性从载荷解析的唯一标识（文章id/doc_id/url 唯一段等），同一链接同一行；哈希由应用层计算、PG 只存储；选定不可换';
COMMENT ON COLUMN data_source.adapter IS '产出本行的列表适配器编码快照';
COMMENT ON COLUMN data_source.raw IS '列表响应中本条目的未解析原文（JSON 片段/HTML 分段）；record_key 预映射与详情抓取目标从本列解析；解析重放依据';
COMMENT ON COLUMN data_source.detail_adapter IS '详情适配器编码（列表即全文时与自身同源）';
COMMENT ON COLUMN data_source.write_mode IS '入库规则快照（继承渠道配置）：overwrite 覆盖/skip 不处理/append 增加';
COMMENT ON COLUMN data_source.data_status IS '数据事实：normal 正常/filtered 规则跳过/dead 源站已删/paywalled 付费墙；新增取值须同步本注释';
COMMENT ON COLUMN data_source.exec_status IS '执行进度：pending 待运行/running 运行中/failed 失败/success 成功';
COMMENT ON COLUMN data_source.extra IS '执行态约定键（fail_count/next_retry_at/last_error）；新增约定键须先确认';
COMMENT ON COLUMN data_source.create_time IS '创建时间（首次入库）';
COMMENT ON COLUMN data_source.update_time IS '修改时间（同步水位）';

CREATE INDEX data_source_sync_idx ON data_source (update_time, id);   -- 分发双水位

-- ============================================================
-- data_detail 数据详情表（资源抓取产出的原始内容，source : detail = 1 : N）
-- 只记录未解析原始载荷（raw），解析产物归分析侧；行存在 = 详情已抓取
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
        -- 详情适配器编码快照：实际抓取本详情的实现类（重放依据）
    raw           bytea NOT NULL,         -- 原始数据：未解析的原始载荷，格式不限（文本/二进制/图片/
                                          --   复合或自定义结构），格式定义与解码在 adapter、PG 只存字节；
                                          --   解析重放基于本列，解析产物归分析侧
    write_mode    text NOT NULL DEFAULT 'overwrite'
        CHECK (write_mode IN ('overwrite', 'skip', 'append')),
        -- 入库规则快照（继承渠道配置）
    data_status   text NOT NULL DEFAULT 'normal'
        CHECK (data_status IN ('normal', 'filtered', 'dead', 'paywalled')),
        -- 数据事实（语义同 data_source.data_status）
    exec_status   text NOT NULL DEFAULT 'success'
        CHECK (exec_status IN ('pending', 'running', 'failed', 'success')),
        -- 执行进度：行由成功抓取产出，落行即 success
    extra         jsonb NOT NULL DEFAULT '{}',
        -- 执行态约定键（fail_count/next_retry_at/last_error）；新增约定键须先与项目所有者确认
    create_time   timestamptz NOT NULL DEFAULT now(),
    update_time   timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT data_detail_uk UNIQUE (source_id, record_key),   -- 同一资源下内容键唯一
    CONSTRAINT data_detail_fk FOREIGN KEY (source_id)
        REFERENCES data_source (id) ON DELETE CASCADE
);
COMMENT ON TABLE data_detail IS '数据详情表：资源产出的原始内容，source:detail=1:N；行存在=详情已抓取';
COMMENT ON COLUMN data_detail.id IS '代理主键';
COMMENT ON COLUMN data_detail.channel_id IS '归属渠道（冗余，与 source 一致）';
COMMENT ON COLUMN data_detail.source_id IS '来源资源（data_source.id）';
COMMENT ON COLUMN data_detail.record_key IS '内容唯一键「渠道前缀id:sha256」：可含时间窗（榜单按天/快讯按条），同一资源可产出多份内容；哈希由应用层计算；选定不可换';
COMMENT ON COLUMN data_detail.fetch_url IS '实际请求地址（抓取态快照）';
COMMENT ON COLUMN data_detail.adapter IS '产出本行的详情适配器编码快照';
COMMENT ON COLUMN data_detail.raw IS '详情原始载荷：bytea 任意格式（文本/二进制/图片/自定义结构），格式定义与解码在 adapter、PG 只存字节';
COMMENT ON COLUMN data_detail.write_mode IS '入库规则快照（继承渠道配置）：overwrite 覆盖/skip 不处理/append 增加';
COMMENT ON COLUMN data_detail.data_status IS '数据事实：normal 正常/filtered 规则跳过/dead 源站已删/paywalled 付费墙；新增取值须同步本注释';
COMMENT ON COLUMN data_detail.exec_status IS '执行进度：行由成功抓取产出，默认 success';
COMMENT ON COLUMN data_detail.extra IS '执行态约定键；新增约定键须先确认';
COMMENT ON COLUMN data_detail.create_time IS '创建时间（首次入库）';
COMMENT ON COLUMN data_detail.update_time IS '修改时间（同步水位）';

CREATE INDEX data_detail_sync_idx ON data_detail (update_time, id);   -- 分发双水位

-- ============================================================
-- sys_node 节点注册表（分布式架构的设施表）
-- 全部服务节点（crawler / process / admin）的心跳与状态登记
-- ============================================================
CREATE TABLE IF NOT EXISTS sys_node (
    id           bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    -- ── 机器标识 ──
    node_uuid    uuid NOT NULL UNIQUE,
                                           -- 节点唯一标识：首次启动生成、持久化到本地文件（容器挂持久卷），
                                           --   重启/pod 重建不变（进程身份）；同一机器可跑多节点（uuid 区分）
    machine       jsonb,
                                           -- 运行环境标识（尽力提供，可空、无唯一承诺）：
                                           --   裸机 {"kind":"host","id":"<machine-id 哈希>"} /
                                           --   docker {"kind":"docker","container":"...","compose_service":"..."} /
                                           --   k8s {"kind":"k8s","deployment":"...","pod":"...","node":"..."}；
                                           --   值取自环境变量/Downward API，不做硬件探测；
                                           --   容器下识别"同 Deployment 多副本"，裸机下识别同机多节点
    node_type    text NOT NULL
        CHECK (node_type IN ('crawler', 'process', 'admin')),
                                           -- 服务类型：哪个服务的节点
    -- ── 运行状态 ──
    status       text NOT NULL DEFAULT 'online'
        CHECK (status IN ('online', 'offline', 'draining')),
                                           -- online 在线 / offline 离线（心跳超时被巡检标记）/
                                           --   draining 优雅下线中（不再认领新任务，跑完手头的退出）
    heartbeat_at timestamptz NOT NULL,
                                           -- 最近一次心跳时间（DB 时钟 now()）：节点周期性 UPDATE 本字段，
                                           --   在线判定 = heartbeat_at > now() - 超时窗口
    -- ── 节点信息 ──
    hostname     text,
                                           -- 主机名（展示与排障用）
    ip           text,
                                           -- 内网 IP（展示用；服务间不走直连，控制经库）
    started_at   timestamptz,
                                           -- 本进程启动时间（区分同一 uuid 的多次运行）
    version      text,
                                           -- 服务版本号（灰度发布/版本混跑排查用）
    extra        jsonb NOT NULL DEFAULT '{}',
        -- 节点扩展信息：运行指标快照（在跑任务数/内存等）等；
        --   新增约定键须先与项目所有者确认
    create_time  timestamptz NOT NULL DEFAULT now(),
    update_time  timestamptz NOT NULL DEFAULT now()
);
COMMENT ON TABLE sys_node IS '节点注册表：全部服务节点的心跳与状态登记（设施表）';
COMMENT ON COLUMN sys_node.id IS '代理主键';
COMMENT ON COLUMN sys_node.node_uuid IS '节点唯一标识：首次启动生成并持久化本地（容器挂持久卷），重启/pod 重建不变';
COMMENT ON COLUMN sys_node.machine IS '运行环境标识（尽力提供，可空）：host/docker/k8s 三形态的结构化标签，值取自环境变量/Downward API，不做硬件探测；容器下识别同 Deployment 多副本，裸机下识别同机多节点';
COMMENT ON COLUMN sys_node.node_type IS '服务类型：crawler/process/admin';
COMMENT ON COLUMN sys_node.status IS '节点状态：online 在线/offline 离线（心跳超时标记）/draining 优雅下线中';
COMMENT ON COLUMN sys_node.heartbeat_at IS '最近心跳时间（DB 时钟）：节点周期性 UPDATE；在线判定 = 大于 now()-超时窗口';
COMMENT ON COLUMN sys_node.hostname IS '主机名（展示与排障）';
COMMENT ON COLUMN sys_node.ip IS '内网 IP（展示；服务间控制经库通信，不走直连）';
COMMENT ON COLUMN sys_node.started_at IS '本进程启动时间（区分同一 uuid 多次运行）';
COMMENT ON COLUMN sys_node.version IS '服务版本号（版本混跑排查）';
COMMENT ON COLUMN sys_node.extra IS '节点扩展信息（运行指标快照等）；新增约定键须先确认';
COMMENT ON COLUMN sys_node.create_time IS '创建时间（首次注册）';
COMMENT ON COLUMN sys_node.update_time IS '修改时间';

-- 心跳与在线判定查询
CREATE INDEX sys_node_live_idx ON sys_node (node_type, status, heartbeat_at);
```

## 三、配套机制（sys_node）

- **注册**：启动时 `INSERT ... ON CONFLICT (node_uuid) DO UPDATE SET heartbeat_at=now(), started_at=now(), hostname/ip/version=...`——一语完成注册与复活；
- **心跳**：`UPDATE sys_node SET heartbeat_at=now() WHERE node_uuid=@me`，随 tick 循环发（复用调度循环，不单开线程）；
- **离线判定**：`UPDATE sys_node SET status='offline' WHERE status='online' AND heartbeat_at < now()-@timeout`——由任一节点的巡检任务执行；节点正常退出写 draining → offline，崩溃则靠心跳超时被巡检翻转；
- 状态判定语义：这是哪个进程 → node_uuid（任务认领的 owner 用它）；是否同机/同 Deployment 副本 → machine 标签（host 下比 id、k8s 下比 deployment）；哪个服务、活没活 → node_type + heartbeat_at；节点怎么死的 → offline 由巡检标记（非节点自写）。
