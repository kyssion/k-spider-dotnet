# 通用 Agent Skills

项目使用 [Agent Skills](https://agentskills.io) 开放格式管理技能，不绑定某个 agent 实现。技能是模型无关的目录：一个 `SKILL.md`（YAML frontmatter + Markdown 指令）+ 可选的 `references/`、`scripts/`、`assets/`。

## 目录

| 路径 | 说明 |
|---|---|
| `.agents/skills/<skill-name>/` | 项目级技能唯一来源；每个技能直接放在这一层 |
| `.agents/skills/*/SKILL.md` | 技能入口；`name` 必须与目录名一致，`description` 必填 |
| `.agents/dotnet-skills/manifest.json` | 从 dotnet/skills 同步的受管技能清单、来源与提交 |
| `.claude/skills` | 指向 `../.agents/skills` 的符号链接，供 Claude Code 发现 |
| `opencode.json` | opencode 的 `skills.paths` 指向 `.agents/skills` |

当前 `.agents/skills/` 下有来自 [dotnet/skills](https://github.com/dotnet/skills) 的技能（初始安装 106 个，实际数量以 `.agents/dotnet-skills/manifest.json` 为准）；它们只是技能文件，不是某个 agent 的插件。

## 各 agent 如何发现

| 运行环境 | 发现方式 |
|---|---|
| Codex | 原生扫描仓库根的 `.agents/skills`，无需配置 |
| Claude Code | 读取项目 `.claude/skills`；本仓库用符号链接指向 `.agents/skills` |
| opencode | 读取 `opencode.json` 的 `skills.paths` |
| 其他兼容 Agent Skills 的模型/客户端 | 把技能根目录配置为 `.agents/skills`，或直接读取其中的 `SKILL.md` |

Windows 若不能使用 git 符号链接，可在本地把 `.claude/skills` 建成目录联接（junction），或按所用 agent 的配置直接指向 `.agents/skills`。

## 同步上游

```bash
./scripts/sync-dotnet-skills.sh          # 跟随上游 main
./scripts/sync-dotnet-skills.sh <ref>    # 指定分支或 tag
DOTNET_SKILLS_SOURCE=/path/to/checkout ./scripts/sync-dotnet-skills.sh
DOTNET_SKILLS_REPO=git@github.com:dotnet/skills.git ./scripts/sync-dotnet-skills.sh
```

脚本行为：

- 只复制 `plugins/*/skills/*` 目录，不执行上游脚本；
- 按 `manifest.json` 删除已下线的受管技能；
- 目标目录存在同名但非受管技能时直接报错，避免覆盖自定义技能；
- 同步后更新 `manifest.json` 的提交与技能清单；
- 需要 `git`（在线同步）与 `python3`。

同步上游等于引入新的模型指令，执行前应查看 `git diff`，确认没有与项目规则冲突的内容；技能内附带的脚本仍可能被 agent 在后续任务中执行，需按代码审查标准对待。

## 自定义技能

不经过同步脚本的技能直接放在 `.agents/skills/<name>/` 即可，`manifest.json` 不会记录，也不会在同步时被删除。新增技能须满足 Agent Skills 格式，并保持名称唯一。
