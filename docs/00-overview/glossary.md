# 术语表

> 状态：持续更新
> 最后更新：2026-11-09
> 数据实体语义以 [data-model.md](../10-architecture/data-model.md) 为唯一权威；本文只统一业务语言。

统一用语可以减少人与 AI 之间的歧义。**代码、文档、提交信息、对话中统一使用下表术语。**
机制细节以「对应」列指向的权威文档为准，本文不复述规则。

| 分类 | 术语 | 含义 | 对应 |
|---|---|---|---|
| 平台 | 采集侧（crawler） | 执行 admin 派发的任务包，把外部站点的原始载荷抓回 PostgreSQL | [crawler.md](../10-architecture/crawler.md) |
| 平台 | 处理侧（process） | 消费 crawler 留存的原始载荷，产出结构化/格式化数据 | [process.md](../10-architecture/process.md) |
| 平台 | 控制面（admin） | 中央调度、集群维护、认证、REST 管理 API 与前端托管；不在数据链路上 | [admin.md](../10-architecture/admin.md) |
| 平台 | 展示层（web） | 通过同源 admin API 展示与操作数据的 React 前端 | [web.md](../10-architecture/web.md) |
| 平台 | 源站 / 站点 | 外部数据站点 | `channel_category` 根节点 |
| 平台 | 渠道 | 一个可拉取的数据供给单元 | `channel` |
| 平台 | 适配器（adapter） | 列表 / 详情的抓取实现目录 | `crawler_adapter` |
| 平台 | parser | 驱动抓取的最小解析，隐式绑定 adapter | [crawler.md](../10-architecture/crawler.md) |
| 平台 | 数据集 / 管线（v1） | v1 概念；v2 不建独立实体，由 `channel` + adapter 表达 | 无独立实体 |
| 平台 | 任务包 | admin 派发给节点的一轮执行单元 | `sys_task` + `task_detail` |
| 平台 | 节点 | 执行任务包的 crawler / process 进程 | `sys_node` |
| 数据 | 资源 / 列表条目 | 一条待抓取的资源连接 | `data_source` |
| 数据 | 详情内容 | 一个资源产出的原始内容 | `data_detail` |
| 数据 | 原始载荷（raw） | 未解析的原始字节，source / detail 双侧留存 | [data-model.md](../10-architecture/data-model.md) |
| 数据 | 执行状态（exec_status） | 抓取与详情回填的执行进度；可重建、可重放 | [data-model.md](../10-architecture/data-model.md) |
| 数据 | 数据状态（data_status） | 数据事实（如 filtered / dead / paywalled），与执行进度独立 | [data-model.md](../10-architecture/data-model.md) |
| 数据 | 记录键（record_key） | 资源 / 内容的唯一键，格式为「渠道前缀id:sha256」；精确规则见待确认项 | [data-model.md](../10-architecture/data-model.md) |
| 数据 | 写入模式（write_mode） | overwrite / skip / append 三种入库语义 | [data-model.md](../10-architecture/data-model.md) |
| 数据 | 双键水位 | 按 `(update_time, id)` 升序增量消费，避免同一时间戳漏行 | [data-model.md](../10-architecture/data-model.md) |
| 数据 | 保留期（retention_days） | 原始载荷必须可达的期限，按渠道声明 | [data-model.md](../10-architecture/data-model.md) |
| 调度 | 心跳 | 节点每 10s 经 gRPC 流上报负载；admin 内存维护 `last_seen` | [distributed.md](../10-architecture/distributed.md) |
| 调度 | 对账 | admin 周期扫描非终态任务，判死、重派、取消与任务表治理 | [distributed.md](../10-architecture/distributed.md) |
| 调度 | 探测（probe） | 超时任务先发单次探测，探测失败才判死，避免网络抖动造成双跑 | [distributed.md](../10-architecture/distributed.md) |
| 调度 | 重派 | 运行失败或节点判死后重新派发；达上限转 `abandoned` | [distributed.md](../10-architecture/distributed.md) |
| 调度 | 派发 token（dispatch_token） | 每次派发的唯一 token，防止旧节点的迟到终态覆盖新进度 | [distributed.md](../10-architecture/distributed.md) |
| 调度 | 终态 | 不可再推进的任务状态（success / abandoned / cancelled 等） | [distributed.md](../10-architecture/distributed.md) |
| 认证 | 会话 Cookie | Cookie 认证载体，HttpOnly、SameSite=Strict，名称由配置决定 | [auth.md](../10-architecture/auth.md) |
| 认证 | CSRF / Antiforgery | 写请求必须携带并校验 antiforgery token | [auth.md](../10-architecture/auth.md) |
| 认证 | 默认账号 | `sys_user` 为空时初始化的 admin 账号，部署后必须修改密码 | [auth.md](../10-architecture/auth.md) |
| 前端 | 同源部署 | admin 托管 `web/dist`，生产环境不经跨源代理 | [admin.md](../10-architecture/admin.md) |
| 前端 | SPA fallback | 非静态资源路径回退到 `index.html`，由前端路由接管 | [admin.md](../10-architecture/admin.md) |
| 流程 | ADR | Architecture Decision Record，架构决策记录 | [20-adr/README.md](../20-adr/README.md) |
| 流程 | 规格（Spec） | 功能验收标准与完成边界 | [40-specs/README.md](../40-specs/README.md) |
| 流程 | 待确认项 | 尚未定稿的契约或决策，必须登记后再实现 | [20-adr/README.md](../20-adr/README.md) |
| 流程 | 唯一来源 | 同一个事实只在一处写成正文，其他地方只引用 | [docs/README.md](../README.md)「写作原则」 |
| 流程 | 红线 | 全仓库禁令集合，唯一权威清单 | [AGENTS.md](../../AGENTS.md)「红线」 |
| 流程 | 完成定义（DoD） | 判定任务完成的清单 | [definition-of-done.md](../50-ai/definition-of-done.md) |
| 流程 | 工程门禁 | 构建、格式、测试、文档与依赖检查等必须通过的质量约束 | [testing.md](../30-guides/testing.md)「当前门禁与目标门禁」 |
| 流程 | verify | 交付前必须全绿的全量校验入口 | [dev-workflow.md](../30-guides/dev-workflow.md)「脚本行为」 |
