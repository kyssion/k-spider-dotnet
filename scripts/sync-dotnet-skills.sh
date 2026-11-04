#!/usr/bin/env bash
# 从 dotnet/skills 同步全部插件技能到中立的 .agents/skills 目录。
# 只复制 SKILL.md 技能目录，不执行上游脚本；同步后建议检查 git diff。
set -euo pipefail
cd "$(dirname "$0")/.."

REF="${1:-main}"
SOURCE="${DOTNET_SKILLS_SOURCE:-}"
REPO="${DOTNET_SKILLS_REPO:-https://github.com/dotnet/skills.git}"
TMP_DIR=""

cleanup() {
  if [ -n "$TMP_DIR" ]; then
    rm -rf "$TMP_DIR"
  fi
}
trap cleanup EXIT

if [ -z "$SOURCE" ]; then
  if ! command -v git >/dev/null 2>&1; then
    echo "错误: 需要 git；或通过 DOTNET_SKILLS_SOURCE 指定本地仓库" >&2
    exit 1
  fi
  TMP_DIR="$(mktemp -d)"
  echo "==> 克隆 dotnet/skills ($REF)"
  cloned=0
  for attempt in 1 2 3; do
    if git clone --depth 1 --branch "$REF" "$REPO" "$TMP_DIR" >/dev/null; then
      cloned=1
      break
    fi
    rm -rf "$TMP_DIR"
    TMP_DIR="$(mktemp -d)"
    echo "克隆失败，重试 ($attempt/3)..." >&2
    sleep 2
  done
  if [ "$cloned" -ne 1 ]; then
    echo "错误: 克隆 dotnet/skills 失败；可用 DOTNET_SKILLS_SOURCE 指向本地仓库" >&2
    exit 1
  fi
  SOURCE="$TMP_DIR"
fi

if ! command -v python3 >/dev/null 2>&1; then
  echo "错误: 需要 python3 解析 SKILL.md" >&2
  exit 1
fi

python3 scripts/sync_dotnet_skills.py "$SOURCE" .agents/skills .agents/dotnet-skills
