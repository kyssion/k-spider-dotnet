# 文档索引

本目录是项目的**唯一权威知识源**。代码回答「怎么做」，文档回答「为什么这样做」以及「约定是什么」。

## 目录职责

每个目录的 `README.md` 是该目录的**文件索引**；本文只做目录级索引与事实归属，不重复各目录的清单。
文档模板（ADR / 规格 / 架构骨架）内联在对应目录的 README 或模板文件里。

| 目录索引 | 回答的问题 | 何时更新 |
| --- | --- | --- |
| [00-overview/README.md](00-overview/README.md) | 这个项目是什么？给谁用？下一步做什么？有什么已知问题？ | 目标、范围、路线或问题状态变化时 |
| [10-architecture/README.md](10-architecture/README.md) | 系统怎么分层？契约与机制是什么？ | 结构或契约变化时，**必须与代码同一次提交** |
| [20-adr/README.md](20-adr/README.md) | 某个决策当时为什么这么定？还有哪些待确认事项？ | 做难以回退的决策或新增待确认项时 |
| [30-guides/README.md](30-guides/README.md) | 新人如何上手？怎么跑？怎么测？ | 流程或命令变化时 |
| [40-specs/README.md](40-specs/README.md) | 某个功能做到什么程度算完成？ | 开始做功能前写，完成后补验收结论 |
| [50-ai/README.md](50-ai/README.md) | 人和 AI 如何分工、如何交接？ | 协作流程变化时 |

仓库里还有几类**不属于 `docs/`** 的协作文档。它们只做入口与索引，**不复制 `docs/` 的内容**：

| 位置 | 作用 | 读者 / 加载方式 |
| --- | --- | --- |
| [README.md](../README.md) | 平台一句话定位、模块状态、仓库结构、最短入口 | 第一次接触项目的人 |
| [AGENTS.md](../AGENTS.md) | AI 协作总入口：**红线**（唯一权威清单）+ 代码约定 + 阅读路由 + 技能索引 | AI 代理，每次任务开始必读 |
| [CONTRIBUTING.md](../CONTRIBUTING.md) | 给人看的上手入口：验证要求、变更流程、提交检查清单 | 新贡献者，首次提交前 |
| [SECURITY.md](../SECURITY.md) | 漏洞报告渠道、已知限制、依赖与凭据处理方式 | 报告安全问题时 |
| `.agents/skills/*/SKILL.md` | 任务触发的**领域知识**；受管上游技能归属见 [manifest.json](../.agents/dotnet-skills/manifest.json) | 按任务相关性加载 1~2 个 |
| [check-doc-references.mjs](../scripts/check-doc-references.mjs)、[check-doc-duplication.mjs](../scripts/check-doc-duplication.mjs)、[check-doc-structure.mjs](../scripts/check-doc-structure.mjs)、[check-doc-status.mjs](../scripts/check-doc-status.mjs) | 文档引用、重复、结构、状态守卫 | `./scripts/verify.sh` 自动执行 |
| [check-skills-manifest.mjs](../scripts/check-skills-manifest.mjs) | 受管技能清单与磁盘一致性检查 | `./scripts/verify.sh` 自动执行 |

## 文件命名

- 目录前缀 `00-` `10-` … 用于固定阅读顺序，新增目录请沿用编号（留 10 的间隔便于插入）。
- ADR 与规格文件统一使用四位序号：`0001-xxx.md`；ADR 的「只增不改」规则见
  [20-adr/README.md](20-adr/README.md)「规则」，规格的写法见
  [40-specs/README.md](40-specs/README.md)「命名与状态」。
- 文件名使用英文小写短横线（如 [data-model.md](10-architecture/data-model.md)），正文用中文。
- 文档引用只有一种写法：**Markdown 链接**。行内代码只留给命令、非文档路径与登记过的通用文件名
  （如 `README.md`）—— 可解析到真实文档的行内路径会被引用检查拒绝。**裸文件名**（不带目录）
  同样不允许，确需保留的在 [check-doc-references.mjs](../scripts/check-doc-references.mjs) 的
  `ALLOWED_BARE_NAMES` 登记并写明理由。
- **代码块（三反引号围栏）内是示例与命令，不参与引用检查与重复检查**；代码块开围栏必须带语言标记
  （纯文本用 `text`），由 [check-doc-structure.mjs](../scripts/check-doc-structure.mjs) 校验。
- 每篇文档必须登记在**所属目录 README 的索引表**里；每个目录 README 必须登记到本文的目录索引 ——
  覆盖由 [check-doc-structure.mjs](../scripts/check-doc-structure.mjs) 校验。

## 引用与标题

- **链接文字固定为目标文件名**（不带目录、不包反引号）；索引文件写 `<目录>/README.md`（根 README 写
  `README.md`）；所有链接文字一律是目标文件名。
- **章节引用写成 `[文件](路径)「节名」`**：节名必须是目标文件某个标题的前缀；一处链接可以连续引用多节；
  同一文档内写 `见本文「节名」`。链接目标**不带 `#` 锚点**，不使用 `§`，不把节名塞进链接文字，
  也不用「第 N 节」「第 N 条」指代（标题不编号，序号会随章节调整失效）；不写「见上文 / 见下文」。
- **同一对象只用一种称呼**：文档自称统一写「本文」，跨文档条目用 ID 或标题；不用序号指代、
  `§`、`见上文`、`见下文`。
- **目录写成反引号**（如 `30-guides/`），入口是各目录的 `README.md`；不要用链接指向目录。
- **文档标题不编号**：`docs/` 与 [AGENTS.md](../AGENTS.md) 的正文标题都不使用 `## 1.`、`## 一、`、
  `## 第 N 步：` —— 顺序由正文与有序列表表达；ADR / 规格正文同样适用。

链接文字、章节引用与措辞由 [check-doc-references.mjs](../scripts/check-doc-references.mjs) 校验；
标题编号、目录索引、代码块语言与架构骨架由 [check-doc-structure.mjs](../scripts/check-doc-structure.mjs) 校验。

## 文档状态

除索引自身与模板外，每篇文档开头必须声明状态，避免读者误以为已定稿：

```markdown
> 状态：草案 | 已确认 | 实现中 | 已实现 | 已过时（见 NNNN） | 已取代（见 NNNN） | 已废弃 | 已取消 | 持续更新
> 最后更新：YYYY-MM-DD
```

**只有这一套状态词表**，按文档类型取合法子集：ADR 用 `草案` / `已确认` / `已取代` / `已废弃`；
规格用 `草案` / `已确认` / `实现中` / `已实现` / `已取消`；其他文档用 `草案` / `已确认` / `已过时` / `持续更新`。
`已确认` 只表示方案或决策已确认，**不等于已实现**；实现进度只看根 [README.md](../README.md) 状态表。

`最后更新` 是本文当前版本对应的日期：ADR 记录决策日期，其余记录核对日期（只改措辞或排版不刷新，
编辑时间由 git 记录）。

状态块与日期由 [check-doc-structure.mjs](../scripts/check-doc-structure.mjs) 校验；状态取值由
[check-doc-status.mjs](../scripts/check-doc-status.mjs) 从本节示例行读取后逐个校验（脚本里不抄词表）。

## 写作原则

1. **先结论后论证**：开头三行说清楚读者该记住什么。
2. **一处编写，其余引用**：同一个事实只在一个文件里写成正文，其他地方只写链接 + 一句话说明
   （「权威清单见 X」），**不复制条文、表格、清单或命令块**。副本一定会漂移，
   而漂移后的两处说法会让读者不知道该信哪一份。
   **精确重复的正文行由 [check-doc-duplication.mjs](../scripts/check-doc-duplication.mjs) 在 verify 中拦下**；
   换了措辞的改写机器判不了，只能靠评审。
3. **写约束不写教程**：只记录「必须怎样 / 禁止怎样」，通用技术知识不复制粘贴。
4. **可执行的表述**：把「注意性能」改写为「列表接口 pageSize 上限 100」。
5. **失效即删除**：文档描述的行为被代码推翻时，同一次提交内修正文档，不留矛盾。
6. **现状与规划分开**：写「已有」必须能在代码里指出处；还没落地的写进 [roadmap.md](00-overview/roadmap.md)
   或 `40-specs/`，不得在架构文档里写成现状 —— 读者会把「设计成这样」读成「已经这样」。
7. **未确认不落地**：按 [20-adr/README.md](20-adr/README.md)「规则」登记待确认事项，不把猜测写成事实。
8. **目录 README 是文件索引**：新增/删除文件时，同一次改动更新所属目录 README 和必要的目录索引。

唯一例外是**短命令与 URL 示例原样重复**：[getting-started.md](30-guides/getting-started.md)、
[dev-workflow.md](30-guides/dev-workflow.md)、[definition-of-done.md](50-ai/definition-of-done.md)、
[CONTRIBUTING.md](../CONTRIBUTING.md) 与仓库根 [README.md](../README.md) 为了开箱可用，可以重复
[dev-workflow.md](30-guides/dev-workflow.md)「常用命令」里的同一串命令，但**不得给出第二种写法或补充参数**。
出现第二种写法即视为违规 —— 那才是歧义的来源。

## 内容归属（写之前先查这张表）

新增内容前先确认「这个事实归谁」，写进唯一来源；其他文档只加链接。

| 事实 | 唯一来源 |
| --- | --- |
| 平台定位、目标与非目标、成功标准 | [vision.md](00-overview/vision.md) |
| 模块落地状态、仓库结构 | 根 [README.md](../README.md) |
| 路线图、里程碑与暂不考虑事项 | [roadmap.md](00-overview/roadmap.md) |
| 已知问题、遗留项、历史结论 | [issue-tracker.md](00-overview/issue-tracker.md) |
| 功能与数据源清单 | [features.md](00-overview/features.md) |
| 术语与统一用语 | [glossary.md](00-overview/glossary.md) |
| 模块边界、依赖方向、机制索引 | [system-overview.md](10-architecture/system-overview.md) |
| 业务表语义与写入规则 | [data-model.md](10-architecture/data-model.md) |
| 业务表物理结构 | [001_business_tables.sql](../db/001_business_tables.sql) |
| 分布式机制、节点/任务模型、调度与对账 | [distributed.md](10-architecture/distributed.md) |
| 系统设施表物理结构 | [002_sys_tables.sql](../db/002_sys_tables.sql) |
| 认证行为、接口与配置 | [auth.md](10-architecture/auth.md) |
| 认证表物理结构 | [003_auth_tables.sql](../db/003_auth_tables.sql) |
| crawler 模块机制、适配器、反爬、仓储 | [crawler.md](10-architecture/crawler.md) |
| process 设计与边界 | [process.md](10-architecture/process.md) |
| admin 服务实现 | [admin.md](10-architecture/admin.md) |
| web 前端 | [web.md](10-architecture/web.md) |
| 架构文档骨架（本目录所有架构文档的必备章节） | [10-architecture/README.md](10-architecture/README.md)「技术文档骨架」 |
| 首次跑通（安装 → 启动 → 校验） | [getting-started.md](30-guides/getting-started.md) |
| 常用命令、端口分配、脚本行为、进程收尾、提交与推送、环境排错 | [dev-workflow.md](30-guides/dev-workflow.md) |
| 测试分层、验证闭环各环节、测试命名 | [testing.md](30-guides/testing.md) |
| 新增特性的分步做法 | [add-a-feature.md](30-guides/add-a-feature.md) |
| 技术文档骨架与写法 | [tech-doc-template.md](30-guides/tech-doc-template.md) |
| 变更分类、强制顺序、文档同步矩阵、验证标准、回滚 | [change-protocol.md](50-ai/change-protocol.md) |
| 角色分工、阶段门禁、上下文管理、交接格式、升级机制 | [collaboration-model.md](50-ai/collaboration-model.md) |
| 完成清单（DoD） | [definition-of-done.md](50-ai/definition-of-done.md) |
| 提示词模板（可直接复制的任务模板） | [prompt-library.md](50-ai/prompt-library.md) |
| 技能目录、跨 agent 发现、上游同步 | [skills.md](50-ai/skills.md) |
| ADR 的写法与待确认索引 | [20-adr/README.md](20-adr/README.md) |
| 规格的写法与索引 | [40-specs/README.md](40-specs/README.md) |
| 状态词表与语义 | 本文「文档状态」 |
| 红线（禁令集合）、代码约定、配置优先级、技能路由 | 根 [AGENTS.md](../AGENTS.md) |
| 贡献流程与提交前检查 | [CONTRIBUTING.md](../CONTRIBUTING.md) |
| 安全报告渠道与已知限制 | [SECURITY.md](../SECURITY.md) |

## 阅读路径推荐

- **新加入的开发者**：[vision.md](00-overview/vision.md) → [getting-started.md](30-guides/getting-started.md) → [system-overview.md](10-architecture/system-overview.md)
- **接到一个功能需求**：[40-specs/README.md](40-specs/README.md) → 对应规格文件 → [add-a-feature.md](30-guides/add-a-feature.md)
- **排查一个已知问题**：[issue-tracker.md](00-overview/issue-tracker.md) → 对应权威文档
- **AI 代理（每次任务开始时）**：仓库根 [AGENTS.md](../AGENTS.md) → 本文 → 与任务相关的架构文档

## 归档

v1 代码、部署单元与旧库导出归档在 `src/k-spider-dotnet-old/`，不作现行依据；v1 测试暂留
`src/k-spider-test/`（门禁过渡）。归档目录整体不参与文档引用检查，见
[check-doc-references.mjs](../scripts/check-doc-references.mjs) 的扫描范围说明。
