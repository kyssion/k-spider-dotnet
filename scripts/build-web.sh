#!/usr/bin/env bash
# 构建前端并拷贝产物到 k-spider-web 的 wwwroot ( 保留 .gitkeep 占位 )
set -euo pipefail

# 锚定脚本绝对路径 : 本脚本会被 verify.sh 以相对路径调用 , 下面还要 cd 进 web/ ,
# 所有相对路径必须先换算成绝对路径再用
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR/../web"

if ! command -v pnpm >/dev/null 2>&1; then
  echo "错误 : 未安装 pnpm ( 需要 Node 20+ 与 pnpm )" >&2
  exit 1
fi

echo "==> pnpm install ( 存在锁文件时按锁安装 )"
pnpm install --frozen-lockfile 2>/dev/null || pnpm install

echo "==> pnpm build ( tsc 类型检查 + vite 打包 )"
pnpm build

echo "==> 拷贝产物到 src/k-spider-web/wwwroot"
WWWROOT="$SCRIPT_DIR/../src/k-spider-web/wwwroot"
mkdir -p "$WWWROOT"
find "$WWWROOT" -mindepth 1 -not -name '.gitkeep' -delete
cp -R dist/. "$WWWROOT"

echo "==> build-web ok"
