# 0004 文档治理 v2：词表、索引与守卫拆分

> 状态：已确认
> 最后更新：2026-11-08
> 关联：[docs/README.md](../README.md)、[testing.md](../30-guides/testing.md)、[change-protocol.md](../50-ai/change-protocol.md)
> 影响：文档目录、状态块、ADR 模板、目录索引、`scripts/check-doc-*.mjs`、`scripts/verify.sh`、CI

## 背景

ADR 0003 建立了「文档唯一权威 + 状态标记 + 技能清单」的治理骨架，但随着文档数量增长，暴露出四类问题：

- 文档检查只有一个脚本，引用、索引、状态、骨架、重复、过期路径混在一起；新增检查项时解析规则容易在脚本内部漂移，
  也无法分别说明每条检查的边界；
- 状态词表实际存在多处副本（[docs/README.md](../README.md)、ADR README、规格 README），状态取值只靠人工评审；
  索引表与正文状态不一致时没有机器守卫；
- ADR 模板没有强制「备选项 / 后果 / 重新评估」，决策记录容易退化成公告；
- 已知问题只存在于会话或零散文档，没有 issue-tracker；路线图与模块状态也缺少统一入口；
- 本地 `verify.sh` 与 CI 的检查项不完全同源，缺少格式校验，文档链接文字与章节引用也没有统一守卫。

## 备选项

| 选项 | 优点 | 代价 |
| --- | --- | --- |
| A. 维持单脚本，继续加检查项 | 改动最小，入口少 | 解析规则与豁免清单继续膨胀；新增检查项会互相污染；无法按职责说明边界 |
| B. 拆为引用 / 重复 / 结构 / 状态四个脚本 + 共用 `scripts/lib/` | 职责清晰；解析口径唯一；新增检查项有固定落点；可分别接入 verify 与 CI | 文件数量增加；需要一次性迁移旧规则 |
| C. 引入通用 Markdown linter / 文档框架 | 规则现成、生态成熟 | 不理解本仓库的「唯一权威 / 目录索引 / 待确认索引」规则；新增依赖与配置成本高于收益 |
| D. 只把状态词表统一，不动机器守卫 | 风险最低 | 无法解决索引与正文漂移、链接失效、重复正文等核心问题 |

## 决策

选择 **B**，并把 ADR 0003 的规则具体化为以下可执行约定：

1. **一套状态词表**：`草案 / 已确认 / 实现中 / 已实现 / 已过时（见 NNNN） / 已取代（见 NNNN） / 已废弃 / 已取消 / 持续更新`；
   状态取值由 `check-doc-status.mjs` 从 [docs/README.md](../README.md)「文档状态」的示例行读取，脚本不抄第二份；
2. **索引是副本**：ADR 与规格索引的状态列必须等于正文状态行；ADR 索引按「当前生效 / 历史」分组，
   分组必须与状态一致；
3. **目录索引覆盖**：每篇文档必须登记在所属目录 README，每个目录 README 必须登记到 [docs/README.md](../README.md)；
4. **ADR 必备章节**：新 ADR 必须包含「备选项」「后果（含重新评估触发条件）」；0001–0003 保留原文，不追溯补写；
5. **引用只有一种写法**：Markdown 链接 + 目标文件名作为链接文字；章节引用写成 `[文件](路径)「节名」`，
   不使用 `#` 锚点、`§`、`第 N 节 / 第 N 条`；
6. **守卫拆分**：`check-doc-references.mjs`（引用与措辞）、`check-doc-duplication.mjs`（精确重复行）、
   `check-doc-structure.mjs`（状态块、索引、ADR 章节、骨架、代码块语言、待确认索引）、
   `check-doc-status.mjs`（词表与索引一致性）共用 `scripts/lib/` 的扫描范围、引用解析与文档模型；
7. **verify 与 CI 同源**：同一顺序运行文档四检查、技能清单、前端构建/E2E、后端构建、格式校验与测试；
   本地允许跳过测试/前端时必须在输出中显式标注，不得静默放过；
8. **补齐概览闭环**：新增 [vision.md](../00-overview/vision.md)、[roadmap.md](../00-overview/roadmap.md)、
   [issue-tracker.md](../00-overview/issue-tracker.md)，并新增 [CONTRIBUTING.md](../../CONTRIBUTING.md)、
   [SECURITY.md](../../SECURITY.md) 两个仓库入口。

## 后果

- 正面：文档引用、索引、状态与重复正文有了机器守卫；状态词表只有一份；新 ADR 的决策质量有最低结构保障；
  本地与 CI 的质量口径可对照；已知问题有可追溯的登记处。
- 负面（需要接受的代价）：文档写法约束变多，新增文档需要同时更新所属目录 README；链接文字被固定为文件名，
  可读性略低；四个脚本与共用 lib 增加维护面；严格重复检查会迫使部分指针句改写。
- 需要同步修改的位置：[docs/README.md](../README.md) 的写作规则与内容归属、各目录 README 的索引、
  [docs/30-guides/tech-doc-template.md](../30-guides/tech-doc-template.md) 的八段骨架、
  `scripts/verify.sh`、`.github/workflows/ci.yml`、[AGENTS.md](../../AGENTS.md) 的红线与文档路由。
- 触发重新评估的条件：文档数量或语言种类显著增长导致单仓库守卫明显变慢；出现误报率持续高于 5% 的检查项；
  或者引入正式的文档站点生成器需要另一套链接模型时，重新评估「四脚本 + 自研解析」的组合。

## 待办

- [x] 拆分四个检查脚本并建立 `scripts/lib/`
- [x] 统一状态词表与状态块格式
- [x] 补齐目录索引、ADR 模板与必备章节
- [x] 新增 vision / roadmap / issue-tracker / CONTRIBUTING / SECURITY
- [x] 更新 verify.sh 与 CI
- [ ] 新服务测试落地后，替换 v1 过渡门禁（见 [issue-tracker.md](../00-overview/issue-tracker.md)）
