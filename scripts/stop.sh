#!/usr/bin/env bash
#
# 按端口停止本项目的开发期进程（默认：admin REST :5094、admin gRPC :5095、web :5173、E2E preview :5899）。
# 幂等：没有进程时返回 0；停完复查端口，未释放则非零退出。
#
# 用法：
#   ./scripts/stop.sh
#   ./scripts/stop.sh 5094
#   ./scripts/stop.sh 5094 5173

set -uo pipefail

DEFAULT_PORTS=(5094 5095 5173 5899)

if [ "$#" -gt 0 ]; then
    PORTS=("$@")
else
    PORTS=("${DEFAULT_PORTS[@]}")
fi

if ! command -v lsof >/dev/null 2>&1; then
    echo "✖ 未找到 lsof，无法按端口定位进程" >&2
    exit 1
fi

echo "▶ 停止开发期进程"

for port in "${PORTS[@]}"; do
    pids="$(lsof -nP -iTCP:"$port" -sTCP:LISTEN -t 2>/dev/null | sort -u | tr '\n' ' ' || true)"
    pids="${pids%% }"

    if [ -z "$pids" ]; then
        printf '  · %s 无进程\n' "$port"
        continue
    fi

    # pids 是空格分隔的 PID 列表，这里刻意不加引号以逐个传递
    # shellcheck disable=SC2086
    kill $pids 2>/dev/null || true
    printf '  ✓ %s 已发送终止信号（PID: %s）\n' "$port" "$pids"
done

# 给进程一点退出时间，再验证端口确实释放
sleep 1

failed=0
for port in "${PORTS[@]}"; do
    if lsof -nP -iTCP:"$port" -sTCP:LISTEN -t >/dev/null 2>&1; then
        printf '  ✖ %s 仍在监听\n' "$port" >&2
        printf '     排查：lsof -nP -iTCP:%s -sTCP:LISTEN\n' "$port" >&2
        failed=1
    fi
done

if [ "$failed" -ne 0 ]; then
    printf '\n\033[1;31m✖ 仍有端口未释放\033[0m\n' >&2
    exit 1
fi

printf '\n\033[1;32m✔ 端口已全部释放\033[0m\n'
