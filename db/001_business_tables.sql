-- ============================================================
-- k-spider 业务表物理结构基线 : crawler_adapter + 四业务表
-- 语义与规则唯一权威见 docs/data-model.md
-- 本文件只定义物理结构 / 约束 / 索引 / 注释，不重复规则
-- 幂等脚本 : 全部 IF NOT EXISTS，可重复执行
-- ============================================================

-- ============================================================
-- crawler_adapter 适配器注册表
-- ============================================================
CREATE TABLE IF NOT EXISTS crawler_adapter (
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
COMMENT ON TABLE crawler_adapter IS '适配器注册表：抓取适配器目录，parser 隐式绑定';
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
CREATE TABLE IF NOT EXISTS channel_category (
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
CREATE TABLE IF NOT EXISTS channel (
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
                                           -- 入库规则（取值见 CHECK；语义见 docs/data-model.md）
    retention_days int CHECK (retention_days IS NULL OR retention_days > 0),
                                           -- 保留期声明（天），NULL = 永久；治理见 docs/data-model.md
    -- ── 调度与监控 ──
    schedule       jsonb NOT NULL DEFAULT '{}',   -- 拉取节奏（间隔/cron）；调度语义见 docs/distributed.md
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
COMMENT ON COLUMN channel.write_mode IS '入库规则（取值见 CHECK；语义见 docs/data-model.md），source/detail 行快照来源';
COMMENT ON COLUMN channel.retention_days IS '保留期（天），NULL=永久；治理规则见 docs/data-model.md';
COMMENT ON COLUMN channel.schedule IS '拉取节奏：间隔/cron 等；调度语义见 docs/distributed.md';
COMMENT ON COLUMN channel.status IS '渠道状态：active 启用/paused 已暂停/retired 已退役；新增取值须同步本注释';
COMMENT ON COLUMN channel.extra IS '渠道实例参数：栏目号/拉取窗口/批量上限/凭据引用名（只存引用不存值）等；具体键见 docs/crawler.md 配置章节';
COMMENT ON COLUMN channel.create_time IS '创建时间';
COMMENT ON COLUMN channel.update_time IS '修改时间';

-- ============================================================
-- data_source 数据源列表表（资源登记：每个要抓的资源连接一行）
-- ============================================================
CREATE TABLE IF NOT EXISTS data_source (
    id            bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    channel_id    bigint NOT NULL REFERENCES channel(id),   -- 渠道数字外键
    record_key    text NOT NULL,          -- 资源唯一键；格式与生成规则见 docs/data-model.md
    adapter       text NOT NULL,
        -- 列表适配器编码快照：实际产出本行的实现类（溯源：谁拉到了这条数据）
    raw           bytea NOT NULL,         -- 列表条目原始载荷；解析重放依据，边界见 docs/data-model.md
    detail_adapter text NOT NULL,
        -- 详情适配器编码：本条数据的详情怎么抓的实现类；
        --   「列表即全文」场景与自身同源
    write_mode    text NOT NULL DEFAULT 'overwrite'
        CHECK (write_mode IN ('overwrite', 'skip', 'append')),
        -- 入库规则快照；语义见 docs/data-model.md
    data_status   text NOT NULL DEFAULT 'normal'
        CHECK (data_status IN ('normal', 'filtered', 'dead', 'paywalled')),
        -- 数据事实（取值见 CHECK；语义见 docs/data-model.md）
    exec_status   text NOT NULL DEFAULT 'pending'
        CHECK (exec_status IN ('pending', 'running', 'failed', 'success')),
        -- 执行进度（取值见 CHECK；语义见 docs/data-model.md）
    extra         jsonb NOT NULL DEFAULT '{}',
        -- 执行态约定键（fail_count/next_retry_at/last_error）；语义见 docs/data-model.md
    create_time   timestamptz NOT NULL DEFAULT now(),
    update_time   timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT data_source_uk UNIQUE (channel_id, record_key)   -- 去重键，语义见 docs/data-model.md
);
COMMENT ON TABLE data_source IS '数据源列表表：资源登记，每个要抓的资源连接一行';
COMMENT ON COLUMN data_source.id IS '代理主键';
COMMENT ON COLUMN data_source.channel_id IS '归属渠道（channel.id）';
COMMENT ON COLUMN data_source.record_key IS '资源唯一键；格式与生成规则见 docs/data-model.md';
COMMENT ON COLUMN data_source.adapter IS '产出本行的列表适配器编码快照';
COMMENT ON COLUMN data_source.raw IS '列表条目原始载荷；解析重放依据，边界见 docs/data-model.md';
COMMENT ON COLUMN data_source.detail_adapter IS '详情适配器编码（列表即全文时与自身同源）';
COMMENT ON COLUMN data_source.write_mode IS '入库规则快照；语义见 docs/data-model.md';
COMMENT ON COLUMN data_source.data_status IS '数据事实（取值见 CHECK；语义见 docs/data-model.md）';
COMMENT ON COLUMN data_source.exec_status IS '执行进度（取值见 CHECK；语义见 docs/data-model.md）';
COMMENT ON COLUMN data_source.extra IS '执行态约定键（fail_count/next_retry_at/last_error）；语义见 docs/data-model.md';
COMMENT ON COLUMN data_source.create_time IS '创建时间（首次入库）';
COMMENT ON COLUMN data_source.update_time IS '修改时间（分发水位，规则见 docs/data-model.md）';

CREATE INDEX IF NOT EXISTS data_source_sync_idx ON data_source (update_time, id);   -- 双键水位（规则见 docs/data-model.md）

-- ============================================================
-- data_detail 数据详情表（资源抓取产出的原始内容，source : detail = 1 : N）
-- 语义与边界见 docs/data-model.md；行存在 = 详情已抓取
-- ============================================================
CREATE TABLE IF NOT EXISTS data_detail (
    id            bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    channel_id    bigint NOT NULL,        -- 冗余渠道（与 source 一致，便于按渠道查询/归档）
    source_id     bigint NOT NULL REFERENCES data_source(id),
                                          -- 来源资源（一个资源可产出多份内容）
    record_key    text NOT NULL,          -- 内容唯一键；格式与生成规则见 docs/data-model.md
    fetch_url     text NOT NULL,          -- 抓取态快照：实际请求的地址（可能经重定向）
    adapter       text NOT NULL,
        -- 详情适配器编码快照：实际抓取本详情的实现类（重放依据）
    raw           bytea NOT NULL,         -- 详情原始载荷：bytea 任意格式；解析重放依据，边界见 docs/data-model.md
    write_mode    text NOT NULL DEFAULT 'overwrite'
        CHECK (write_mode IN ('overwrite', 'skip', 'append')),
        -- 入库规则快照；语义见 docs/data-model.md
    data_status   text NOT NULL DEFAULT 'normal'
        CHECK (data_status IN ('normal', 'filtered', 'dead', 'paywalled')),
        -- 数据事实（取值见 CHECK；语义同 data_source.data_status）
    exec_status   text NOT NULL DEFAULT 'success'
        CHECK (exec_status IN ('pending', 'running', 'failed', 'success')),
        -- 执行进度（取值见 CHECK；语义见 docs/data-model.md）
    extra         jsonb NOT NULL DEFAULT '{}',
        -- 执行态约定键（fail_count/next_retry_at/last_error）；语义见 docs/data-model.md
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
COMMENT ON COLUMN data_detail.record_key IS '内容唯一键；格式与生成规则见 docs/data-model.md';
COMMENT ON COLUMN data_detail.fetch_url IS '实际请求地址（抓取态快照）';
COMMENT ON COLUMN data_detail.adapter IS '产出本行的详情适配器编码快照';
COMMENT ON COLUMN data_detail.raw IS '详情原始载荷：bytea 任意格式；解析重放依据，边界见 docs/data-model.md';
COMMENT ON COLUMN data_detail.write_mode IS '入库规则快照；语义见 docs/data-model.md';
COMMENT ON COLUMN data_detail.data_status IS '数据事实（取值见 CHECK；语义见 docs/data-model.md）';
COMMENT ON COLUMN data_detail.exec_status IS '执行进度（取值见 CHECK；语义见 docs/data-model.md）';
COMMENT ON COLUMN data_detail.extra IS '执行态约定键；语义见 docs/data-model.md';
COMMENT ON COLUMN data_detail.create_time IS '创建时间（首次入库）';
COMMENT ON COLUMN data_detail.update_time IS '修改时间（分发水位，规则见 docs/data-model.md）';

CREATE INDEX IF NOT EXISTS data_detail_sync_idx ON data_detail (update_time, id);   -- 双键水位（规则见 docs/data-model.md）
