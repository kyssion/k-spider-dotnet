# k-spider 分布式架构方案

> admin 中央调度 + crawler 节点执行。本文为 sys_node / sys_task 语义、状态机与调度机制的唯一权威；物理结构落 `db/002_sys_tables.sql`。业务表见 [data-model.md](data-model.md)，平台架构见 [architecture.md](architecture.md)。

> **状态**：机制已由项目所有者逐项确认（2026-10）；协议于 2026-11 确认为 **gRPC 双向流**。标注【待定】的为实现期再定事项，未定稿前不落代码。

## 一、设计目标与形态

- **目标形态**：admin 作为中央调度器与集群维护工具（心跳、状态、web 后台管理页面），crawler 为执行节点横向扩缩；admin 读 `channel.schedule` 规划并派发任务包；
- **admin 单机**：只负责任务分发、调度与后台管理，并发量小；保持**调度无状态**（调度现场全在库：sys_node / sys_task），重启后靠节点重连与对账恢复——单机 + 自动重启优先于主从，主从为远期可选项；认证密钥环按 [auth.md](auth.md) 持久化，不属于调度状态；
- **节点即牲口**：crawler 无本地持久状态（业务进度在业务表、任务状态在 sys_task），随时可杀可换；**不做优雅下线（draining）**，异常退出靠任务重派收敛；
- **演进**：process 服务将来以 `node_type='process'` 接入同一套节点与任务机制。

## 二、节点模型（sys_node）

| 关注点 | 约定 |
|---|---|
| 身份 | `node_id` = 节点启动时生成的 uuid，**不持久化**——节点重启即新身份。代价（重启引发在跑任务重派 churn、无法关联"同一台机器"）已确认接受 |
| 注册 | 节点启动后主动向 admin 发起 gRPC `Connect` 双向流；admin 以流绑定 `node_id`，`INSERT ... ON CONFLICT (node_id) DO UPDATE` 完成注册与复活 |
| 心跳 | 走 **gRPC 流上 heartbeat（10s）**，携带负载快照（在跑任务数）；admin 内存维护 `last_seen`，超时判死；**心跳不写库** |
| 事实源 | **admin 内存为准，sys_node 是镜像**（后台展示用）；admin 重启后花名册短暂空窗，节点重连与心跳数秒内自动回满 |
| 状态 | 只有 `online`（运行中）/ `offline`（已下线）；offline 是删除前的短暂墓碑态（防瞬时抖动误删），admin 随后清删行 |
| device | jsonb 硬件信息（ipv4/ipv6/host/mac/cpu 核数/内存/磁盘/带宽等），节点自报、尽力提供、可空——仅展示与容量评估，**不作调度依据**（mac/带宽在云与容器环境常不可得，调度依赖的数据必须可靠） |
| ip / port | 仅展示/诊断（可由连接远端地址或节点自报填充）；**不作调度依据**——gRPC 由节点主动连 admin，节点不监听入站端口 |
| extra | jsonb 扩展信息（注册时自报）；新增约定键须先与项目所有者确认 |

## 三、任务模型（sys_task）

- **粒度 = 任务包**：一个渠道一轮一行任务；`task_detail`(jsonb) 装任务全部内容（channel_id、轮次类型、批量上限、时间窗、运行上限快照等）。**不逐条建任务**——详情回填一轮数千条，逐条建行差三个数量级；包内推进靠业务表 `exec_status`，节点自治；
- **派发**：admin 先 `INSERT sys_task`（`pushing`），再通过该节点的 gRPC 流下发 `task_dispatch`；写流成功置 `pushed`，失败置 `push_failed`；
- **节点直写状态**：crawler 直接 UPDATE sys_task 维护运行状态与进度（与直写业务表同模式），不经 admin 接口；终态通过流上 `task_result` 唤醒 admin；
- **node_id 不建外键**：节点行会被清删而任务行还在，只存引用；
- **状态时间不入列**：各状态时间戳、进度、fail_count、派发 token 全记 extra；`update_time` 随状态与进度回报更新，兼对账超时判据（见六）。

状态机（admin 写推送系 / 取消 / 重派，节点写运行系与终态）：

| 状态 | 写入方 | 语义 |
|---|---|---|
| `pushing` 推送中 | admin | 任务已落库，正在推送 |
| `push_failed` 推送失败 | admin | 推送未送达（重试或改派） |
| `pushed` 推送成功 | admin | 节点已收到，待节点接手 |
| `running` 运行中 | 节点 | 执行中；每批回报进度 touch update_time |
| `run_failed` 运行失败 | 节点 | 本轮执行失败，待 admin 重派 |
| `success` 运行成功 | 节点 | **终态** |
| `abandoned` 失败放弃 | admin | **终态**：重派达上限（fail_count） |
| `cancelled` 已取消 | admin | **终态**：渠道暂停 / 人工取消 |

**防竞态写入**：节点写终态用条件更新——

```sql
UPDATE sys_task SET status='success', ...
WHERE id=@task AND status='running';   -- 影响行数 0 → 状态已被 admin 改过（取消/重派），放弃写入
```

一条规则兜住两个场景：取消通知丢失时节点写终态自然发现被取消、安全收敛；重派后旧节点的迟到终态不覆盖新节点进度。更严谨可在 extra 放派发 token 进条件【待定：实现时定】。

## 四、通信协议

- **gRPC 双向流长连接**（HTTP/2）：节点启动后主动 `Connect` 到 admin，连接存活即节点存活；**节点零入站端口**，admin 不需要直连节点；
- **契约项目**：proto 与生成代码放 `src/k-spider-grpc/`，admin / crawler 共同引用；`task_detail` 以 JSON 字符串原样传递，各服务不共享实体项目；
- **上行**：`register` / `heartbeat`（10s，负载快照）/ `task_result`（终态唤醒）/ `probe_reply`；
- **下行**：`register_ack` / `task_dispatch` / `task_cancel` / `probe_request`；
- **监听**：admin 单端口 Kestrel `Http1AndHttp2`，同承 REST 管理 API 与 gRPC；内网默认 h2c 明文，TLS 配置位预留；每连接写操作加锁序列化下行；
- **重连**：节点断线后指数退避重连（1/2/5/10/30s），重连成功即重新绑定 `node_id`；
- 接口路径与 proto 字段细节【待定：实现时定】。

## 五、调度规则

- **派发互斥**：同一渠道同一时刻只派发一个任务（admin 内存互斥 + 重启后从 sys_task 非终态恢复互斥映射）——一条规则同时消灭"同渠道重复派发"与"多节点同源 QPS 叠加"；
- **节奏**：admin 读 `channel.schedule` 生成渠道轮任务包；
- **节点选择**：依据心跳负载快照（在跑任务数）做简单均衡；
- **全局限速数值**：归 crawler 抓取配置（源/渠道级令牌桶），不在此层。

## 六、对账（admin 核心循环）

admin 周期扫描非终态任务，纠正"库里说的"与"实际发生的"偏差：

- **统一判据**：`status 非终态 AND update_time < now() - 无活动超时`。节点跑批时每批回报进度 touch update_time（进度写 extra），因此一个索引扫描同时覆盖"节点死了"与"任务卡死了"两类故障（健康长任务不会误杀）；
- **探测失败 ≠ 执行失败**：对超时任务先在流上发 `probe_request`（5s deadline），重试 3 次 / 持续 30s 仍不可达才判死重派——防网络抖动把正在跑的任务重发导致双跑；
- **重派**：run_failed 或节点判死 → 回退状态重发（fail_count+1）；达上限（默认 3）转 `abandoned` 终态（防坏任务无限重派占住渠道互斥位）；
- **admin 重启**：节点重连后重新注册；非终态任务重启后做一轮主动探测对齐（节点后续直写也会自然收敛）；
- **参数不硬编码**：探测重试 K/T、无活动超时、对账周期、重派上限是 admin 循环级参数，走 admin 配置；运行上限可按渠道 / 任务类型覆盖，派发时算好快照进 task_detail。**不放 sys_task.extra**——每行复制循环参数既冗余又改不动（改了只影响新任务）。

## 七、取消

admin 改状态为 `cancelled` + 流上发 `task_cancel`（快路径）；通知丢失由条件更新兜底（节点写终态时影响行数为 0，自然停）。取消后是否补发新任务包由调度另行决定，不混入取消变迁。

## 八、重派安全前提

对账重发模型成立的架构级假设：**任务重复执行无副作用**——overwrite 模式 upsert 幂等、append 模式 record_key 含时间窗（见 [data-model.md](data-model.md) 的写入规则）。crawler 实现必须保证此性质。

## 九、故障行为边界

| 场景 | 行为 |
|---|---|
| admin 宕机 | 节点继续跑已领任务（状态直写 PG），新任务停发；admin 重启后节点重连、从 sys_node / sys_task 恢复现场 |
| 节点宕机 | gRPC 流断开 / 心跳超时判死 → sys_node 置 offline 墓碑后清删；名下非终态任务由对账重派 |
| 网络抖动 | 流自动重连；探测 K/T 内不判死，不产生双跑 |
| 推送丢失 | 流断开导致 `task_dispatch` 未送达，任务停在 pushing / push_failed；对账超时后重派，或节点重连后由对账补推 |

## 十、任务表治理

admin 定期 DELETE 终态且 update_time 超过 N 天（默认 7 天，配置）的行；**非终态行永不清**（对账对象）。量级 = 渠道数 × 轮次频率，撑不起按天分区，普通 DELETE 够用。

## 十一、待定事项

| 事项 | 说明 |
|---|---|
| proto 契约 | 消息字段、错误码与流控细节（实现时定） |
| TLS | 内网 h2c 明文优先；TLS 配置位预留 |
| 对账参数默认值 | K/T、无活动超时、对账周期、重派上限（admin 配置落地时定） |
| k-spider-admin 重建 | 项目结构恢复 admin 服务（调度循环 + gRPC 服务端 + REST 管理 API + web 后台） |
