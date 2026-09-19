#!/usr/bin/env bash
# 文档链接检查 : 校验 Markdown 里的相对链接都存在 ( 纯离线 , 无外部依赖 )
# 文档腐化最常见的形态就是改了文件名 / 挪了目录却没改引用 , 这里做一道机械兜底 ;
# 内容层面的"文档与代码是否一致"仍靠 AGENTS.md 的同步规则与 docs/README.md 的映射表人工保证。
set -euo pipefail
cd "$(dirname "$0")/.."

missing_file=$(mktemp)
trap 'rm -f "$missing_file"' EXIT

while IFS= read -r file; do
    dir=$(dirname "$file")
    # 从 ](link) 中取出路径部分 ( 去掉锚点 ) , 只检查 .md 链接
    while IFS= read -r link; do
        [ -z "$link" ] && continue
        case "$link" in
            http* | mailto*) continue ;;
        esac
        [ -e "$dir/$link" ] || echo "缺失链接 : $file -> $link" >> "$missing_file"
    done < <(grep -oh "]([^)]*\.md\(#[^)]*\)\?)" "$file" 2>/dev/null | sed 's/](//; s/)$//; s/#.*$//' || true)
done < <(find . -name "*.md" -not -path "./.git/*" -not -path "*/obj/*" -not -path "*/bin/*" | sort)

if [ -s "$missing_file" ]; then
    cat "$missing_file"
    echo "==> 文档链接检查失败"
    exit 1
fi

echo "==> 文档链接检查通过"
