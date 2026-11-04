# .NET Agent Skills

来源：<https://github.com/dotnet/skills>
同步提交、安装时间与技能清单见 `manifest.json`（当前 107 个）。

技能本体在项目根 `.agents/skills/<skill-name>/`，遵循 Agent Skills 标准（`SKILL.md` + 可选 `references/` / `scripts/` / `assets/`）。

同步：

```bash
node scripts/sync-dotnet-skills.mjs                         # 跟随上游 main
node scripts/sync-dotnet-skills.mjs --check                 # 只检查是否有更新
node scripts/sync-dotnet-skills.mjs --ref v1.0.0            # 指定分支 / 标签 / 提交
node scripts/sync-dotnet-skills.mjs --source ../checkout    # 用本地检出
node scripts/sync-dotnet-skills.mjs --force                 # 版本未变也重写
node scripts/sync-dotnet-skills.mjs --allow-plugin-removal  # 确认上游插件下线
```

同步脚本解析上游插件清单后只复制技能目录，不执行上游脚本。已安装清单见 `manifest.json`；不要直接修改这些受管技能，更新会在下次同步时覆盖。其他非 .NET 的自定义技能可以放在 `.agents/skills/` 下，不会被本脚本删除。

许可证：MIT，见本目录 `LICENSE`。
