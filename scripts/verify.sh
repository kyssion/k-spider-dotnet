#!/usr/bin/env bash
#
# 全量校验：交付前必须全绿的唯一入口。
#
# 与 .github/workflows/ci.yml 同源：同一质量口径（构建、格式与测试都用 Release）；CI 按后端 / 前端 / 文档并行。
# 允许的质量差异只有一处，且必须在此登记 —— 不登记的差异就是「本地绿、CI 红」的起点：
#   1. 测试跳过：--skip-tests 只用于本地快速冒烟（跳过前端单元 / E2E 与后端测试），输出会显式标注；
#      CI 永远跑完整测试，不提供跳过开关。
#
# 用法：
#   ./scripts/verify.sh              # 全量
#   ./scripts/verify.sh --skip-tests # 本地冒烟（文档 + 技能 + 前端静态 + 构建 + 格式）

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOLUTION="$ROOT_DIR/k-spider-dotnet.sln"
WEB_DIR="$ROOT_DIR/web"

SKIP_TESTS="false"
for arg in "$@"; do
  case "$arg" in
    --skip-tests) SKIP_TESTS="true" ;;
    *) echo "未知参数：$arg" >&2; exit 2 ;;
  esac
done

step() {
  printf '\n\033[1;36m▶ %s\033[0m\n' "$1"
}

fail() {
  printf '\n\033[1;31m✖ %s\033[0m\n' "$1" >&2
  exit 1
}

printf '\033[1mk-spider 全量校验\033[0m\n'

command -v dotnet >/dev/null 2>&1 || fail "未找到 dotnet，请先安装 .NET SDK（版本由 global.json 锁定）"
command -v node >/dev/null 2>&1 || fail "未找到 node，请先安装 Node.js 20+"
command -v pnpm >/dev/null 2>&1 || fail "未找到 pnpm，请先安装 Node.js 20+ 与 pnpm"

# ── 文档 ────────────────────────────────────────────────────────────────
# 四类问题都不会让任何编译或测试红灯，只能靠人读到：
#   1. 引用失效与写法漂移（指向已删除/重命名的路径、链接文字/章节引用不合约定）；
#   2. 同一段正文写成两份（docs/README.md「一处编写，其余引用」）；
#   3. 结构缺失（状态块/日期、目录索引、ADR 章节、骨架、标题编号、代码块语言）；
#   4. 状态不一致（状态词表、索引与正文、ADR 分组、规格验收结论）。
# 守门逻辑与豁免规则见各脚本头部注释，共用的解析与文档模型在 scripts/lib/。
step "1/10 文档引用检查"
node "$ROOT_DIR/scripts/check-doc-references.mjs" || fail "文档引用了不存在的路径或写法不合约定（见上方清单）"

step "2/10 文档重复检查"
node "$ROOT_DIR/scripts/check-doc-duplication.mjs" || fail "同一段正文出现在多处（见上方清单）"

step "3/10 文档结构检查"
node "$ROOT_DIR/scripts/check-doc-structure.mjs" || fail "文档结构不完整（见上方清单）"

step "4/10 文档状态检查"
node "$ROOT_DIR/scripts/check-doc-status.mjs" || fail "索引与正文状态不一致（见上方清单）"

# ── 技能 ────────────────────────────────────────────────────────────────
# 受管技能的存在性只记在 manifest.json 里：删掉目录、改坏清单都不会让编译或测试红灯，
# 只有等到任务按 AGENTS.md 的路由去加载时才暴露。此检查纯离线。
step "5/10 技能清单检查"
node "$ROOT_DIR/scripts/check-skills-manifest.mjs" || fail "技能清单与磁盘不一致（见上方清单）"

# ── 前端 ────────────────────────────────────────────────────────────────
step "6/10 前端校验（install + lint / format / build / 体积）"
bash "$ROOT_DIR/scripts/build-web.sh" || fail "前端构建失败"
(cd "$WEB_DIR" && pnpm run lint && pnpm run format:check && pnpm run check:bundle) || fail "前端静态校验失败"

if [ "$SKIP_TESTS" = "true" ]; then
  step "7/10 前端单元测试与 E2E（已跳过）"
else
  step "7/10 前端单元测试与 E2E（Vitest + Playwright）"
  (cd "$WEB_DIR" && pnpm run test && pnpm test:e2e) || fail "前端测试失败"
fi

# ── 后端 ────────────────────────────────────────────────────────────────
# 用 Release 与 CI 对齐：Debug 下不成立的裁剪、条件编译与优化差异，本地不该漏过。
step "8/10 后端构建（Release）"
dotnet build "$SOLUTION" --configuration Release --nologo -v quiet || fail "后端构建失败"

step "9/10 后端格式校验"
dotnet format "$SOLUTION" --no-restore --verify-no-changes || fail "后端格式与 .editorconfig 不一致，请执行 dotnet format"

if [ "$SKIP_TESTS" = "true" ]; then
  step "10/10 v2 守卫 + v1 过渡测试（已跳过）"
else
  # v2 守卫在解决方案内，复用第 8 步的 Release 构建；v1 测试暂留过渡门禁，待服务测试落地后替换。
  # v1 测试项目不在 sln 中，不能用 --no-build，保证测试项目按 Release 重建。
  step "10/10 v2 守卫 + v1 过渡测试（Release，排除 Live）"
  dotnet test "$SOLUTION" --configuration Release --no-build --nologo -v quiet || fail "v2 守卫测试失败"
  dotnet test "$ROOT_DIR/src/k-spider-test/k-spider-test.csproj" --configuration Release --filter "TestCategory!=Live" --nologo -v quiet || fail "v1 过渡测试失败"
fi

if [ "$SKIP_TESTS" = "true" ]; then
  printf '\n\033[1;33m✔ 校验通过（测试已跳过，仅本地冒烟；交付前必须跑全量）\033[0m\n'
else
  printf '\n\033[1;32m✔ 全部校验通过\033[0m\n'
fi
