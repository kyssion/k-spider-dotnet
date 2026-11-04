# 通用 Agent Skills

项目使用 [Agent Skills](https://agentskills.io) 开放格式管理技能，不绑定某个 agent 实现。技能是模型无关的目录：一个 `SKILL.md`（YAML frontmatter + Markdown 指令）+ 可选的 `references/`、`scripts/`、`assets/`。

> **状态**：现状（最后核对：2026-11-04）。

## 目录

| 路径 | 说明 |
|---|---|
| `.agents/skills/<skill-name>/` | 项目级技能唯一来源；每个技能直接放在这一层 |
| `.agents/skills/*/SKILL.md` | 技能入口；`name` 必须与目录名一致，`description` 必填 |
| `.agents/dotnet-skills/manifest.json` | 从 dotnet/skills 同步的受管技能清单、来源与提交 |
| `opencode.json` | opencode 的 `skills.paths` 指向 `.agents/skills` |

当前 `.agents/skills/` 下有来自 [dotnet/skills](https://github.com/dotnet/skills) 的技能（初始安装 106 个，实际数量以 `.agents/dotnet-skills/manifest.json` 为准）；它们只是技能文件，不是某个 agent 的插件。

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
node scripts/sync-dotnet-skills.mjs          # 跟随上游 main
node scripts/sync-dotnet-skills.mjs <ref>    # 指定分支或 tag
DOTNET_SKILLS_SOURCE=/path/to/checkout node scripts/sync-dotnet-skills.mjs
DOTNET_SKILLS_REPO=git@github.com:dotnet/skills.git node scripts/sync-dotnet-skills.mjs
```

脚本行为：

- 只复制 `plugins/*/skills/*` 目录，不执行上游脚本；
- 按 `manifest.json` 删除已下线的受管技能；
- 目标目录存在同名但非受管技能时直接报错，避免覆盖自定义技能；
- 同步后更新 `manifest.json` 的提交与技能清单；
- 需要 Node.js 20+；在线同步还需要 `git`。

同步上游等于引入新的模型指令，执行前应查看 `git diff`，确认没有与项目规则冲突的内容；技能内附带的脚本仍可能被 agent 在后续任务中执行，需按代码审查标准对待。

## 自定义技能

不经过同步脚本的技能直接放在 `.agents/skills/<name>/` 即可，`manifest.json` 不会记录，也不会在同步时被删除。新增技能须满足 Agent Skills 格式，并保持名称唯一。
