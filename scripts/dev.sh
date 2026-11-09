#!/usr/bin/env bash
#
# 同时启动 admin（:5094）与 web 开发服务器（:5173）。
# Ctrl+C 会同时停止两个进程；后台启动的进程用 ./scripts/stop.sh 收尾。
#
# 用法：./scripts/dev.sh

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

command -v dotnet >/dev/null 2>&1 || { echo "未找到 dotnet" >&2; exit 1; }
command -v pnpm >/dev/null 2>&1 || { echo "未找到 pnpm" >&2; exit 1; }

pids=()

cleanup() {
    echo ""
    echo "▶ 正在停止服务…"
    for pid in "${pids[@]:-}"; do
        if [ -n "$pid" ] && kill -0 "$pid" 2>/dev/null; then
            kill "$pid" 2>/dev/null || true
        fi
    done
    wait 2>/dev/null || true
    echo "▶ 已停止"
}

trap cleanup EXIT INT TERM

echo "▶ 启动 admin http://localhost:5094"
(cd "$ROOT_DIR" && dotnet run --project src/k-spider-admin --launch-profile http) &
pids+=($!)

echo "▶ 启动 web http://localhost:5173"
(cd "$ROOT_DIR/web" && pnpm dev) &
pids+=($!)

echo "▶ 按 Ctrl+C 停止全部服务"
# bash 3.2（macOS 默认）没有 wait -n，失败时退化为等待全部子进程
wait -n 2>/dev/null || wait
