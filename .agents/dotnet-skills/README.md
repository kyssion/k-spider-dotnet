# .NET Agent Skills

来源：<https://github.com/dotnet/skills>
同步提交、安装时间与技能清单见 `manifest.json`（初始安装 106 个）。

技能本体在项目根 `.agents/skills/<skill-name>/`，遵循 Agent Skills 标准（`SKILL.md` + 可选 `references/` / `scripts/` / `assets/`）。

同步：

```bash
./scripts/sync-dotnet-skills.sh          # 跟随上游 main
./scripts/sync-dotnet-skills.sh <ref>    # 指定分支或 tag
DOTNET_SKILLS_SOURCE=/path/to/checkout ./scripts/sync-dotnet-skills.sh
```

同步脚本只复制技能目录，不执行上游脚本。已安装清单见 `manifest.json`；不要直接修改这些受管技能，更新会在下次同步时覆盖。其他非 .NET 的自定义技能可以放在 `.agents/skills/` 下，不会被本脚本删除。

许可证：MIT，见本目录 `LICENSE`。
