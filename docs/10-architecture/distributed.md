# k-spider 分布式架构方案

> 状态：已确认
> 最后更新：2026-11-08

本文定义 `sys_node` / `sys_task` 语义、状态机、调度、对账、取消与故障边界；物理结构以
[002_sys_tables.sql](../../db/002_sys_tables.sql) 为唯一权威。业务表、平台架构与认证见本文「相关文档」。

## 定位与职责

admin 是中央调度器与集群维护工具，crawler 是执行节点。admin 读 `channel.schedule` 规划渠道轮任务包并按节点负载派发；crawler 只执行收到的任务包，不自行发现渠道、不自行定时。调度现场全在库（`sys_node` / `sys_task`），admin 重启后靠节点重连与对账恢复。

设计取舍：admin 单机 + 自动重启优先于主从；节点即牲口，无本地持久状态，随时可杀可换；不做优雅下线（draining），异常退出靠任务重派收敛。process 将来以 `node_type='process'` 接入同一套节点与任务机制。

## 结构与约定

### 节点模型（sys_node）

| 关注点 | 约定 |
|---|---|
| 身份 | `node_id` = 节点启动时生成的 uuid，**不持久化**；节点重启即新身份。代价（重启引发在跑任务重派 churn、无法关联“同一台机器”）已确认接受 |
| 注册 | 节点启动后主动向 admin 发起 gRPC `Connect` 双向流；admin 以流绑定 `node_id`，`INSERT ... ON CONFLICT (node_id) DO UPDATE` 完成注册与复活 |
| 心跳 | 走 gRPC 流上 heartbeat（10s），携带负载快照（在跑任务数）；admin 内存维护 `last_seen`，超时判死；**心跳不写库** |
| 事实源 | **admin 内存为准，sys_node 是镜像**（后台展示用）；admin 重启后花名册短暂空窗，节点重连与心跳数秒内自动回满 |
| 状态 | 只有 `online`（运行中）/ `offline`（已下线）；offline 是删除前的短暂墓碑态（防瞬时抖动误删），admin 随后清删行 |
| device | jsonb 硬件信息（ipv4/ipv6/host/mac/cpu 核数/内存/磁盘/带宽等），节点自报、尽力提供、可空——仅展示与容量评估，**不作调度依据**（mac/带宽在云与容器环境常不可得，调度依赖的数据必须可靠） |
| ip / port | 仅展示/诊断（可由连接远端地址或节点自报填充）；**不作调度依据**——gRPC 由节点主动连 admin，节点不监听入站端口 |
| extra | jsonb 扩展信息（注册时自报）；新增约定键须先与项目所有者确认 |

### 任务模型（sys_task）

- **粒度 = 任务包**：一个渠道一轮一行任务；`task_detail`（jsonb）装任务全部内容（channel_id、轮次类型、批量上限、时间窗、运行上限快照等）。**不逐条建任务**——详情回填一轮数千条，逐条建行差三个数量级；包内推进靠业务表 `exec_status`，节点自治；
- **派发**：admin 先 `INSERT sys_task`（`pushing`），再通过该节点的 gRPC 流下发 `task_dispatch`；写流成功置 `pushed`，失败置 `push_failed`；
- **派发 token（必需）**：每次派发生成唯一 token 写入 `extra`；节点所有状态、进度与终态更新都必须带 `dispatch_token=@token` 条件；admin 重派时生成新 token。没有 token，旧节点的迟到终态会在新节点已进入 `running` 后覆盖新进度；
- **节点直写状态**：crawler 直接 UPDATE `sys_task` 维护运行状态与进度（与直写业务表同模式），不经 admin 接口；终态通过流上 `task_result` 唤醒 admin（携带 token）；
- **node_id 不建外键**：节点行会被清删而任务行还在，只存引用；
- **状态时间不入列**：各状态时间戳、进度、`fail_count`、派发 token 全记 `extra`；`update_time` 随状态与进度回报更新，兼对账超时判据。

### 状态机

admin 写推送系 / 取消 / 重派，节点写运行系与终态：

| 状态 | 写入方 | 语义 |
|---|---|---|
| `pushing` 推送中 | admin | 任务已落库，正在推送 |
| `push_failed` 推送失败 | admin | 推送未送达（重试或改派） |
| `pushed` 推送成功 | admin | 节点已收到，待节点接手 |
| `running` 运行中 | 节点 | 执行中；每批回报进度，touch `update_time` |
| `run_failed` 运行失败 | 节点 | 本轮执行失败，待 admin 重派 |
| `success` 运行成功 | 节点 | **终态** |
| `abandoned` 失败放弃 | admin | **终态**：重派达上限（`fail_count`） |
| `cancelled` 已取消 | admin | **终态**：渠道暂停 / 人工取消 |

### 防竞态写入

节点写终态用条件更新：

```sql
UPDATE sys_task SET status='success', ...
WHERE id=@task
  AND status='running'
  AND extra->>'dispatch_token' = @token;
-- 影响行数 0 → 状态或派发代次已变（取消/重派），放弃写入
```

条件同时覆盖两个场景：取消通知丢失时节点写终态自然发现被取消、安全收敛；重派后旧节点的迟到终态因 token 不匹配而不会覆盖新节点进度。仅靠 `status='running'` 不够——新节点重派后也会进入 `running`。

## 核心流程

### 注册与心跳

节点启动 → `Connect` 双向流 → admin 绑定 `node_id` 并注册/复活 → 节点每 10s 发 heartbeat（携带在跑任务数）→ admin 内存更新 `last_seen`。心跳超时判死，`sys_node` 置 offline 墓碑后清删。

### 派发

admin 读 `channel.schedule` 生成渠道轮任务包 → 先落 `sys_task`（`pushing`）→ 选节点（按心跳负载快照做简单均衡）→ 流上 `task_dispatch` → 成功置 `pushed`，失败置 `push_failed`。同一渠道同一时刻只派发一个任务（admin 内存互斥 + 重启后从非终态恢复互斥映射）；同源站跨渠道的 QPS 叠加由 crawler 的 host 级限流兜底。

### 对账

admin 周期扫描非终态任务，统一判据：`status` 非终态 AND `update_time < now() - 无活动超时`。节点跑批时每批回报进度 touch `update_time`，因此一个索引扫描同时覆盖“节点死了”与“任务卡死了”，健康长任务不会误杀。

探测失败 ≠ 执行失败：对超时任务先在流上发 `probe_request`（单次探测 5s 超时、按请求关联应答，不是给整条长连接设 deadline），重试 3 次 / 持续 30s 仍不可达才判死重派，防网络抖动把正在跑的任务重发导致双跑。

### 重派与取消

- `run_failed` 或节点判死 → 回退状态重发（`fail_count+1`）；达上限（默认 3）转 `abandoned` 终态，防坏任务无限重派占住渠道互斥位；
- 取消：admin 改状态为 `cancelled` + 流上发 `task_cancel`（快路径）；节点收到后取消当前执行上下文，并在每批 / 每页 / 每次请求前检查取消。通知丢失由条件更新兜底，但这只阻止状态写入，不阻止已开始的执行，因此取消检查点必须显式实现；
- 取消后是否补发新任务包由调度另行决定，不混入取消变迁。

### 故障行为

| 场景 | 行为 |
|---|---|
| admin 宕机 | 节点继续跑已领任务（状态直写 PG），新任务停发；admin 重启后节点重连、从 `sys_node` / `sys_task` 恢复现场 |
| 节点宕机 | gRPC 流断开 / 心跳超时判死 → `sys_node` 置 offline 墓碑后清删；名下非终态任务由对账重派 |
| 网络抖动 | 流自动重连；探测 K/T 内不判死，不产生双跑 |
| 推送丢失 | 流断开导致 `task_dispatch` 未送达，任务停在 `pushing` / `push_failed`；对账超时后重派，或节点重连后由对账补推 |

### 任务表治理

admin 定期 DELETE 终态且 `update_time` 超过 N 天（默认 7 天，配置）的行；**非终态行永不清**（对账对象）。量级 = 渠道数 × 轮次频率，撑不起按天分区，普通 DELETE 够用。

### 重派安全前提

对账重发模型成立的架构级假设：**任务重复执行无副作用**——overwrite 模式 upsert 幂等、append 模式 `record_key` 含时间窗（见 [data-model.md](data-model.md) 的写入规则）。crawler 实现必须保证此性质。

## 数据与接口

### gRPC 双向流

- HTTP/2 长连接：节点启动后主动 `Connect` 到 admin，连接存活即节点存活；**节点零入站端口**，admin 不需要直连节点；
- 契约项目：proto 与生成代码计划放 `src/k-spider-grpc/`（待创建），admin / crawler 共同引用；`task_detail` 以 JSON 字符串原样传递，各服务不共享实体项目；
- 上行：`register` / `heartbeat`（10s，负载快照）/ `task_result`（终态唤醒）/ `probe_reply`；
- 下行：`register_ack` / `task_dispatch` / `task_cancel` / `probe_request`；
- 每连接写操作加锁序列化下行；节点断线后指数退避重连（1/2/5/10/30s），重连成功即重新绑定 `node_id`。

### 监听端点

REST 管理 API 与 gRPC 使用**分离的监听端点**：REST 走 HTTP/1.1，gRPC 端点只承载 HTTP/2。明文单端口 `Http1AndHttp2` 无法同时协商 h2c gRPC，不能作为实现方案；内网 gRPC 默认 h2c 明文，TLS 按端点预留。

### 数据表

`sys_node` / `sys_task` 的物理结构见 [002_sys_tables.sql](../../db/002_sys_tables.sql)；语义与状态机以本文为准。节点直写数据库，数据库凭证与节点认证属于同一信任边界。

## 配置

- 对账参数：探测重试 K/T、无活动超时、对账周期、重派上限是 admin 循环级参数，走 admin 配置；运行上限可按渠道 / 任务类型覆盖，派发时算好快照进 `task_detail`。**不放 `sys_task.extra`**——每行复制循环参数既冗余又改不动；
- `channel.schedule`：admin 读取的渠道调度配置；JSON 键、类型、时区与错过轮次语义尚未定稿，实现前确认；
- 任务表保留天数（默认 7 天）走 admin 配置；
- 全局限速数值归 crawler 抓取配置（源/渠道级令牌桶），不在 admin 调度层，避免两处限流口径。

## 测试与守门

- 状态机、派发 token、条件更新、对账重派、取消检查点必须有测试；
- gRPC 契约测试与 `task_detail` / `extra` JSON 契约测试在协议定稿后补齐；
- 节点认证与端口拆分属于部署验收项；
- 文档与 DDL 的引用检查见 [testing.md](../30-guides/testing.md)「测试分层」。

## 待确认事项

| 事项 | 说明 |
|---|---|
| proto 契约 | 消息字段、错误码与流控细节（实现时定） |
| `task_detail` / `extra` JSON 契约 | 键名、类型、枚举与 casing；`dispatch_token` 键名先随 proto 定稿 |
| 端口号与 TLS 配置 | 具体监听端口号、证书/密钥来源；协议分离已由 ADR 0002 确认 |
| 节点认证 | mTLS / 共享 token / 网络隔离的选型（实现前确认） |
| 同渠道互斥持久化 | `channel_id` 在 `task_detail` JSONB 中；恢复映射需解析 JSONB，是否提升为独立列并加索引（实现时定） |
| 对账参数默认值 | K/T、无活动超时、对账周期、重派上限（admin 配置落地时定） |
| admin 重建 | 项目结构恢复 admin 服务（调度循环 + gRPC 服务端 + REST 管理 API + web 后台） |

## 相关文档

- [system-overview.md](system-overview.md)、[data-model.md](data-model.md)、[auth.md](auth.md)：相邻契约；
- [admin.md](admin.md)：调度与对账的实现落点；
- [002_sys_tables.sql](../../db/002_sys_tables.sql)：系统设施表物理结构；
- [0002-grpc-transport.md](../20-adr/0002-grpc-transport.md)：传输与端口拆分决策。
