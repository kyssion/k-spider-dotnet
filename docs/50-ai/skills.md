# 通用 Agent Skills

> 状态：持续更新
> 最后更新：2026-11-09

项目使用 [Agent Skills](https://agentskills.io) 开放格式管理技能，不绑定某个 agent 实现。技能是模型无关的目录：一个 `SKILL.md`（YAML frontmatter + Markdown 指令）+ 可选的 `references/`、`scripts/`、`assets/`。

> `SKILL.md` 格式遵循 Agent Skills 开放标准；`.agents/skills/` 是仓库约定，宿主是否自动发现取决于客户端。opencode 通过 `opencode.json` 适配。

## 标准与目录位置

- [AGENTS.md](../../AGENTS.md) 是已确立的跨工具协作标准；
- `SKILL.md` 的格式有开放规范，但 `.agents/skills/` 这个目录位置目前只是厂商中立的开放草案，采用面窄；
- 技能正文与目录位置解耦，迁移成本仅为 `git mv`。

受管技能在文档引用检查中按 manifest **逐个排除**，不按目录前缀整体排除；原因是上游技能里的示例路径指向上游仓库，逐条改写会破坏同步能力。

## 目录

| 路径 | 说明 |
|---|---|
| `.agents/skills/<skill-name>/` | 项目级技能唯一来源；每个技能直接放在这一层 |
| `.agents/skills/*/SKILL.md` | 技能入口；`name` 必须与目录名一致，`description` 必填 |
| `.agents/dotnet-skills/manifest.json` | 从 dotnet/skills 同步的受管技能清单、来源与提交 |
| `opencode.json` | opencode 的 `skills.paths` 指向 `.agents/skills` |

当前 `.agents/skills/` 下有来自 [dotnet/skills](https://github.com/dotnet/skills) 的技能（数量以 `.agents/dotnet-skills/manifest.json` 为准）；它们只是技能文件，不是某个 agent 的插件。

清单与磁盘的一致性（每个受管目录存在且含 `SKILL.md`，自有技能也必须可被发现）由
`scripts/check-skills-manifest.mjs` 在 verify 中离线校验；同步脚本的 `--check` 需要联网，平时跑不到。
清单路径与 schema 统一由 `scripts/lib/skills-manifest.mjs` 定义，检查与同步共用。

## 各 agent 如何发现

| 运行环境 | 发现方式 |
|---|---|
| Codex | 原生扫描仓库根的 `.agents/skills`，无需配置 |
| Claude Code | 默认不提交 agent 专用目录；需要时在本地创建 `.claude/skills -> ../.agents/skills`，或把技能目录链接/复制到 `.claude/skills` |
| opencode | 读取 `opencode.json` 的 `skills.paths` |
| 其他兼容 Agent Skills 的模型/客户端 | 把技能根目录配置为 `.agents/skills`，或直接读取其中的 `SKILL.md` |

`.claude/` 是本地可选的 Claude Code 适配层，不是项目技能来源；不提交，避免把某个 agent 的发现路径变成仓库约束。

## 同步上游

```bash
node scripts/sync-dotnet-skills.mjs                         # 跟随上游 main
node scripts/sync-dotnet-skills.mjs --check                 # 只检查是否有更新
node scripts/sync-dotnet-skills.mjs --ref v1.0.0            # 指定分支 / 标签 / 提交
node scripts/sync-dotnet-skills.mjs --source ../checkout    # 用本地检出
node scripts/sync-dotnet-skills.mjs --force                 # 版本未变也重写
node scripts/sync-dotnet-skills.mjs --allow-plugin-removal  # 确认上游插件下线
```

脚本行为：

- 解析上游 `marketplace.json` + 插件 `plugin.json`，不按目录硬拷贝；上游格式变化会整体失败，不会静默少导；
- 只复制技能目录，不执行上游脚本；
- 按 `manifest.json` 删除已下线的受管技能；插件消失需要显式 `--allow-plugin-removal`；
- 目标目录存在同名但非受管技能时直接报错，避免覆盖自定义技能；
- 同步后更新 `manifest.json` 的提交、插件归属与技能清单；
- 需要 Node.js 20+；在线同步还需要 `git`。

同步上游等于引入新的模型指令，执行前应查看 `git diff`，确认没有与项目规则冲突的内容；技能内附带的脚本仍可能被 agent 在后续任务中执行，需按代码审查标准对待。

## 自定义技能

不经过同步脚本的技能直接放在 `.agents/skills/<name>/` 即可，`manifest.json` 不会记录，也不会在同步时被删除。新增技能须满足 Agent Skills 格式，并保持名称唯一。

当前有 2 个项目自有技能：

- [SKILL.md](../../.agents/skills/k-spider-docs-contract/SKILL.md)：文档 / 契约 / ADR / 待确认变更的判别与易漏项；
- [SKILL.md](../../.agents/skills/k-spider-grpc-contract/SKILL.md)：gRPC proto / 契约项目变更流程。

crawler adapter、write_mode、反爬策略等项目知识在 M3 真实适配器落地后再沉淀；新增自有技能时按上面的
目录规则放置，并在 [AGENTS.md](../../AGENTS.md)「技能」表补一行路由。
