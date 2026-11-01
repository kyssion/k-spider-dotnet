-- ============================================================
-- k-spider 系统设施表物理结构基线 : sys_node / sys_task
-- 语义、状态机与调度机制唯一权威见 docs/distributed.md
-- 本文件只定义物理结构 / 约束 / 索引 / 注释，不重复规则
-- 幂等脚本 : 全部 IF NOT EXISTS，可重复执行
-- ============================================================

-- ============================================================
-- sys_node 节点注册表（admin 内存花名册的镜像）
-- ============================================================
CREATE TABLE IF NOT EXISTS sys_node (
    id           bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    node_id      uuid NOT NULL UNIQUE,
                                           -- 节点唯一标识：启动时生成，不持久化（节点即牲口，
                                           --   重启即新身份）；名下任务靠 sys_task 对账重派收敛
    node_type    text NOT NULL
        CHECK (node_type IN ('admin', 'crawler', 'process')),
                                           -- 服务类型：admin 调度器 / crawler 抓取节点 /
                                           --   process 加工节点（预留，同一套机制接入）
    status       text NOT NULL DEFAULT 'online'
        CHECK (status IN ('online', 'offline')),
                                           -- online 运行中 / offline 已下线（删除前墓碑态，
                                           --   admin 随后清删行）；在线判定在 admin 内存
                                           --   （网络心跳），本表只是镜像
    ip           text,
                                           -- 节点地址：仅展示/诊断（可由连接远端地址或节点
                                           --   自报填充），不作调度依据
    port         int,
                                           -- 节点端口：仅展示/诊断；gRPC 由节点主动连 admin，
                                           --   节点不监听入站端口
    device       jsonb,
                                           -- 硬件信息（ipv4/ipv6/host/mac/cpu 核数/内存/磁盘/
                                           --   带宽等），节点自报、尽力提供、可空；仅展示与
                                           --   容量评估，不作调度依据
    extra        jsonb NOT NULL DEFAULT '{}',
        -- 节点扩展信息（注册时自报）；语义见 docs/distributed.md
    create_time  timestamptz NOT NULL DEFAULT now(),
    update_time  timestamptz NOT NULL DEFAULT now()
);
COMMENT ON TABLE sys_node IS '节点注册表：admin 内存花名册的镜像（在线判定在 admin 网络心跳，不入库）';
COMMENT ON COLUMN sys_node.id IS '代理主键';
COMMENT ON COLUMN sys_node.node_id IS '节点唯一标识：启动时生成、不持久化（重启即新身份），唯一';
COMMENT ON COLUMN sys_node.node_type IS '服务类型：admin 调度器 / crawler 抓取节点 / process 加工节点（预留）';
COMMENT ON COLUMN sys_node.status IS '节点状态：online 运行中 / offline 已下线（删除前墓碑态，admin 清删）';
COMMENT ON COLUMN sys_node.ip IS '节点地址：仅展示/诊断，不作调度依据';
COMMENT ON COLUMN sys_node.port IS '节点端口：仅展示/诊断；gRPC 无节点入站端口';
COMMENT ON COLUMN sys_node.device IS '硬件信息（ipv4/ipv6/host/mac/cpu/内存/磁盘/带宽等）：节点自报、尽力提供、可空，不作调度依据';
COMMENT ON COLUMN sys_node.extra IS '节点扩展信息（注册时自报）；语义见 docs/distributed.md';
COMMENT ON COLUMN sys_node.create_time IS '创建时间（首次注册）';
COMMENT ON COLUMN sys_node.update_time IS '修改时间（注册与状态变更时更新；心跳只进 admin 内存）';

-- ============================================================
-- sys_task 任务表（admin 中央调度的任务包，一渠道一轮一行）
-- ============================================================
CREATE TABLE IF NOT EXISTS sys_task (
    id           bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    node_id      uuid NOT NULL,
                                           -- 执行节点（sys_node.node_id 引用）；
                                           --   不建外键：节点行会被清删而任务行还在
    task_type    text NOT NULL,
                                           -- 任务类型（渠道列表轮 / 详情回填轮 / 平台任务等，
                                           --   取值随 crawler 实现注册，开放集合由代码校验）
    task_detail  jsonb NOT NULL,
                                           -- 任务包内容（channel_id/轮次类型/批量上限/时间窗/
                                           --   运行上限快照等）；粒度与语义见 docs/distributed.md
    status       text NOT NULL DEFAULT 'pushing'
        CHECK (status IN ('pushing', 'push_failed', 'pushed', 'running',
                          'run_failed', 'success', 'abandoned', 'cancelled')),
                                           -- 任务状态（取值见 CHECK；状态机见 docs/distributed.md）
    extra        jsonb NOT NULL DEFAULT '{}',
        -- 状态时间戳、进度、fail_count、派发 token；语义见 docs/distributed.md
    create_time  timestamptz NOT NULL DEFAULT now(),
    update_time  timestamptz NOT NULL DEFAULT now()
                                           -- 随状态与进度回报更新；对账判据见 docs/distributed.md
);
COMMENT ON TABLE sys_task IS '任务表：admin 中央调度的任务包（一渠道一轮一行），节点直写运行状态';
COMMENT ON COLUMN sys_task.id IS '代理主键';
COMMENT ON COLUMN sys_task.node_id IS '执行节点（sys_node.node_id 引用，不建外键：节点行会被清删）';
COMMENT ON COLUMN sys_task.task_type IS '任务类型：渠道列表轮/详情回填轮/平台任务等（开放集合，代码注册表校验）';
COMMENT ON COLUMN sys_task.task_detail IS '任务包内容（channel_id/轮次类型/批量上限/时间窗/运行上限快照等）；语义见 docs/distributed.md';
COMMENT ON COLUMN sys_task.status IS '任务状态（取值见 CHECK；状态机见 docs/distributed.md）';
COMMENT ON COLUMN sys_task.extra IS '状态时间戳、进度、fail_count、派发 token；语义见 docs/distributed.md';
COMMENT ON COLUMN sys_task.create_time IS '创建时间（任务发布）';
COMMENT ON COLUMN sys_task.update_time IS '修改时间：随状态与进度回报更新；对账判据见 docs/distributed.md';

-- 对账扫描索引路径（判据语义见 docs/distributed.md）
CREATE INDEX IF NOT EXISTS sys_task_reconcile_idx ON sys_task (status, update_time);

-- 节点维度：节点判死后重派其名下非终态任务
CREATE INDEX IF NOT EXISTS sys_task_node_idx ON sys_task (node_id, status);
