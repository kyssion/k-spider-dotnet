#!/usr/bin/env python3
"""把 dotnet/skills 的插件技能收敛到中立的 .agents/skills 目录。"""

import json
import re
import shutil
import sys
from datetime import datetime, timezone
from pathlib import Path

NAME_PATTERN = re.compile(r"^[a-z0-9][a-z0-9-]{0,63}$")


def parse_frontmatter(skill_file: Path) -> tuple[str, str]:
    text = skill_file.read_text(encoding="utf-8")
    if not text.startswith("---"):
        raise ValueError(f"{skill_file} 缺少 YAML frontmatter")

    parts = text.split("---", 2)
    if len(parts) < 3:
        raise ValueError(f"{skill_file} 的 frontmatter 未闭合")

    name = None
    description = None
    for line in parts[1].splitlines():
        if line.startswith("name:"):
            name = line.split(":", 1)[1].strip().strip("\"'")
        elif line.startswith("description:"):
            description = line.split(":", 1)[1].strip().strip("\"'")

    if not name:
        raise ValueError(f"{skill_file} 缺少 name")
    if not description:
        raise ValueError(f"{skill_file} 缺少 description")
    return name, description


def load_manifest(path: Path) -> dict:
    if not path.exists():
        return {}
    return json.loads(path.read_text(encoding="utf-8"))


def main() -> int:
    if len(sys.argv) != 4:
        print("用法: sync_dotnet_skills.py <source-repo> <skills-dir> <metadata-dir>", file=sys.stderr)
        return 2

    source = Path(sys.argv[1]).resolve()
    skills_dir = Path(sys.argv[2]).resolve()
    metadata_dir = Path(sys.argv[3]).resolve()
    manifest_path = metadata_dir / "manifest.json"

    plugin_skills = sorted(source.glob("plugins/*/skills/*/SKILL.md"))
    if not plugin_skills:
        raise SystemExit(f"未在 {source} 找到任何插件技能")

    discovered: list[dict] = []
    seen: dict[str, Path] = {}
    for skill_file in plugin_skills:
        name, _ = parse_frontmatter(skill_file)
        if not NAME_PATTERN.fullmatch(name):
            raise SystemExit(f"技能名不合法: {name} ({skill_file})")
        if name != skill_file.parent.name:
            raise SystemExit(f"技能名与目录不一致: {name} != {skill_file.parent.name}")
        if name in seen:
            raise SystemExit(f"技能名重复: {name} ({seen[name]} / {skill_file})")
        seen[name] = skill_file
        plugin = skill_file.parents[2].name
        discovered.append({
            "name": name,
            "plugin": plugin,
            "path": skill_file.parent.relative_to(source).as_posix(),
        })

    previous = load_manifest(manifest_path)
    managed = {item["name"] for item in previous.get("skills", [])}
    incoming = {item["name"] for item in discovered}

    skills_dir.mkdir(parents=True, exist_ok=True)
    for stale in sorted(managed - incoming):
        shutil.rmtree(skills_dir / stale, ignore_errors=True)

    for item in discovered:
        destination = skills_dir / item["name"]
        if destination.exists() and item["name"] not in managed:
            raise SystemExit(
                f"目标已存在且不属于本脚本管理: {destination}; 请改名或移走后再同步"
            )
        if destination.exists():
            shutil.rmtree(destination)
        shutil.copytree(source / item["path"], destination)

    metadata_dir.mkdir(parents=True, exist_ok=True)
    license_file = source / "LICENSE"
    if license_file.exists():
        shutil.copy2(license_file, metadata_dir / "LICENSE")

    commit = "unknown"
    try:
        import subprocess

        commit = subprocess.check_output(
            ["git", "-C", str(source), "rev-parse", "HEAD"], text=True
        ).strip()
    except Exception:
        pass

    manifest = {
        "source": "https://github.com/dotnet/skills",
        "commit": commit,
        "installedAt": datetime.now(timezone.utc).isoformat(),
        "skills": discovered,
    }
    manifest_path.write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )

    print(f"==> 已同步 {len(discovered)} 个技能 -> {skills_dir}")
    print(f"==> 来源提交: {commit}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
