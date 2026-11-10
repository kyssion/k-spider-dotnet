# 架构决策记录（ADR）

> 状态：已确认
> 最后更新：2026-11-09

本目录登记契约与架构决策；已确认决策的正文仍以对应权威文档为准，决策记录只写「决定了什么、为什么、
影响哪里」。本文同时是 `20-adr/` 的文件索引。

## 为什么要写 ADR

代码能说明「系统现在是什么样」，但不能说明「当时为什么这样，以及否掉了哪些选项」。
半年后的人（以及 AI）最容易犯的错误，是把已经权衡过的决策当成疏漏再改一遍。
ADR 就是为了把「权衡过程」固化下来。

## 什么时候必须写

满足以下任意一条，就**先写 ADR，再写代码**：

- 引入或移除一个框架 / 重要依赖；
- 改变层之间的依赖方向或模块边界；
- 变更对外契约的兼容性（破坏性变更）；
- 选择持久化方案、部署形态、鉴权方案、调度语义；
- 明确决定「不做某件事」（负向决策同样值得记录）。

不需要写：日常业务逻辑实现、局部重构、命名调整、样式微调、文档措辞修改。

## 规则

- 新契约或契约变更：先在「待确认」区登记，向项目所有者确认后再写决策记录并同步权威文档；
- 未确认事项不得写进代码或文档正文当作既定事实，只能以【待确认】标记或「待确认事项」章节形式存在；
- 每份决策必须有状态、日期、决策人与关联权威文档；文件命名与文档状态见 [docs/README.md](../README.md)；
- 决策与权威文档冲突时，以最新已确认决策为准，并在同一次改动中修正权威文档；
- ADR 只增不改；需要改变决策时新增一篇，并在新旧两篇中互相引用；仅允许更新状态行与索引；
- 新 ADR 正文不承载进度或待办：进度归 [roadmap.md](../00-overview/roadmap.md)，问题归 [issue-tracker.md](../00-overview/issue-tracker.md)，门禁判据归 [testing.md](../30-guides/testing.md)；模板不含待办；0004–0008 的旧待办作为历史快照，不再更新；
- 每篇必须包含「备选项」与「后果（含重新评估触发条件）」。没有备选项对比的 ADR 只是公告；
  没有重新评估条件的 ADR 会被永远遵守，哪怕当初的前提早已不成立；
- 新增 ADR 后必须更新本文底部的索引表；
- 索引表是状态的**副本**：状态列必须与正文的状态行一致；分组也必须对应（「当前生效」只能是已确认，
  「历史」只能是已取代 / 已废弃）。由 [check-doc-status.mjs](../../scripts/check-doc-status.mjs) 校验，
  冲突时改正文。

**历史说明**：0001–0003 写于 ADR 0004 之前，保留原文（包括其中的旧脚本名与旧路径），
不追溯补「决策人 / 备选项 / 后果」；0004–0008 的「待办」是旧模板的历史快照（0009 正文只点名 0005–0008；0004 同样适用），不再更新；
新 ADR 一律按本文「模板」执行，现行文档守卫见 [docs/README.md](../README.md)。

## 模板

```markdown
# NNNN 决策标题

> 状态：草案
> 最后更新：YYYY-MM-DD
> 决策人：（人或 AI 会话标识）
> 关联：权威文档链接
> 影响：受影响的服务、表、接口或脚本

## 背景

要解决什么问题，当前做法为什么不够，有哪些约束（团队规模、时间、现有技术栈）。

## 备选项

| 选项 | 优点 | 代价 |
| --- | --- | --- |
| A | | |
| B | | |

## 决策

选择 A。用一两句话说明核心理由。只写结论，不重复权威文档的完整定义。

## 后果

- 正面：
- 负面（需要接受的代价）：
- 需要同步修改的位置：
- 触发重新评估的条件：
```

## 待确认

详细描述只在对应权威文档的待确认章节/条目中维护，本表只做入口索引。
本表只索引契约 / 架构 / 跨模块设计类待确认项；指南中的流程 / 工具待确认项见
[30-guides/README.md](../30-guides/README.md)「指南待确认索引」。

| 事项 | 入口 |
| --- | --- |
| 跨模块架构待确认入口 | [system-overview.md](../10-architecture/system-overview.md) |
| `record_key` 精确格式 | [data-model.md](../10-architecture/data-model.md) |
| `channel.schedule` JSON | [distributed.md](../10-architecture/distributed.md) |
| `channel.extra` 键表 | [crawler.md](../10-architecture/crawler.md) |
| crawler 抓取配置键 | [crawler.md](../10-architecture/crawler.md) |
| 反爬策略细节 | [crawler.md](../10-architecture/crawler.md) |
| `sys_task.task_detail` / `extra` | [distributed.md](../10-architecture/distributed.md) |
| proto 契约与字段 | [0008-grpc-contract-project.md](0008-grpc-contract-project.md) / [distributed.md](../10-architecture/distributed.md) |
| 端口号与 TLS 配置 | [distributed.md](../10-architecture/distributed.md) |
| 同渠道互斥持久化 | [distributed.md](../10-architecture/distributed.md) |
| 对账参数默认值 | [distributed.md](../10-architecture/distributed.md) |
| 节点认证 | [distributed.md](../10-architecture/distributed.md) |
| admin 重建 | [distributed.md](../10-architecture/distributed.md) |
| 认证账号 PATCH 端点拆分 | [auth.md](../10-architecture/auth.md) |
| 权限模型 | [auth.md](../10-architecture/auth.md) |
| 账号恢复方式 | [auth.md](../10-architecture/auth.md) |
| detail 写入快照来源 | [data-model.md](../10-architecture/data-model.md) |
| `data_status` 是否跳过详情回填 | [data-model.md](../10-architecture/data-model.md) |
| 双键水位消费侧 | [crawler.md](../10-architecture/crawler.md) / [process.md](../10-architecture/process.md) |
| 数据访问选型 | [AGENTS.md](../../AGENTS.md) |
| admin REST 首期端点/DTO | [admin.md](../10-architecture/admin.md) / [web.md](../10-architecture/web.md) |
| admin 后台页面范围 | [admin.md](../10-architecture/admin.md) |
| admin 目录结构与模块拆分 | [admin.md](../10-architecture/admin.md) |
| 部署形态与运维 | [system-overview.md](../10-architecture/system-overview.md) / [admin.md](../10-architecture/admin.md) |
| web 首期页面范围与交互 | [web.md](../10-architecture/web.md) |
| 组件库与样式 | [web.md](../10-architecture/web.md) |
| 图表交互 | [web.md](../10-architecture/web.md) |
| 权限粒度 | [web.md](../10-architecture/web.md) |
| 前端 i18n | [web.md](../10-architecture/web.md) |
| 富文本 / HTML 注入策略 | [web.md](../10-architecture/web.md) |
| process 结构化数据契约 | [process.md](../10-architecture/process.md) |
| process 节点接入 | [process.md](../10-architecture/process.md) / [distributed.md](../10-architecture/distributed.md) |
| 解析器注册与版本 | [process.md](../10-architecture/process.md) |
| 原始载荷消费方式 | [process.md](../10-architecture/process.md) |
| `data_detail` 失败行语义 | [data-model.md](../10-architecture/data-model.md) |
| 列表即全文的 detail 行 | [crawler.md](../10-architecture/crawler.md) |

## 索引

### 当前生效

| 编号 | 标题 | 状态 |
| --- | --- | --- |
| [0001-admin-central-scheduling.md](0001-admin-central-scheduling.md) | admin 中央调度 | 已确认 |
| [0002-grpc-transport.md](0002-grpc-transport.md) | gRPC 传输与端口拆分 | 已确认 |
| [0003-docs-and-skill-governance.md](0003-docs-and-skill-governance.md) | 文档与技能治理 | 已确认 |
| [0004-docs-and-skill-governance-v2.md](0004-docs-and-skill-governance-v2.md) | 文档治理 v2：词表、索引与守卫拆分（顺序条款由 0007 细化） | 已确认 |
| [0005-engineering-gates-first.md](0005-engineering-gates-first.md) | 工程基座加固先于分布式实现 | 已确认 |
| [0006-frontend-static-gates.md](0006-frontend-static-gates.md) | 前端静态门禁采用 ESLint + Prettier + Vitest + 体积预算 | 已确认 |
| [0007-ci-parallel-jobs.md](0007-ci-parallel-jobs.md) | CI 按域并行，质量口径与 verify 同源 | 已确认 |
| [0008-grpc-contract-project.md](0008-grpc-contract-project.md) | gRPC 契约项目采用共享生成库 | 已确认 |
| [0009-adr-does-not-track-progress.md](0009-adr-does-not-track-progress.md) | ADR 不承载进度：移除模板待办 | 已确认 |

> 0004–0008 的「待办」是旧模板历史快照，不再更新；状态行已标注，进度见 [roadmap.md](../00-overview/roadmap.md)。
>
> | ADR | 当前进度 |
> | --- | --- |
> | 0004 | roadmap M1 / M2 |
> | 0005 | roadmap M1.5 / M2 |
> | 0006 | roadmap M1.5 |
> | 0007 | roadmap M1.5 |
> | 0008 | roadmap M2 |

### 历史（已取代 / 已废弃）

当前没有被取代或废弃的决策。新增历史条目时保留原记录，并在新旧两篇中互相引用。
