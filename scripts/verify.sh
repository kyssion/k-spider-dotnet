#!/usr/bin/env bash
# 一键验证 : 构建 + 全部单元测试 ( 离线 , 不依赖网络与数据库 ) + 前端构建 ( 本机有 pnpm 时 )
set -euo pipefail
cd "$(dirname "$0")/.."

echo "==> 文档链接检查"
bash scripts/check-docs.sh

# 前端构建 ( 产物进 k-spider-web/wwwroot ) : 无 pnpm 环境时跳过 ( CI 与前端开发必跑 )
if command -v pnpm >/dev/null 2>&1; then
  echo "==> 前端构建"
  bash scripts/build-web.sh
else
  echo "==> 跳过前端构建 ( 未安装 pnpm ; 需要时执行 scripts/build-web.sh )"
fi

# 前端 E2E ( mock API , 离线确定 ) : 需要 pnpm + 已安装 playwright 浏览器 , 缺任一则跳过
if command -v pnpm >/dev/null 2>&1 && { [ -d "$HOME/Library/Caches/ms-playwright" ] || [ -d "$HOME/.cache/ms-playwright" ]; }; then
  echo "==> 前端 E2E 测试"
  (cd web && pnpm test:e2e)
else
  echo "==> 跳过前端 E2E ( 未安装 pnpm 或 playwright 浏览器 ; 首次执行 pnpm --dir web exec playwright install chromium )"
fi

echo "==> dotnet build"
dotnet build k-spider-dotnet.sln

echo "==> dotnet test ( 离线 : 排除 Live 分类的真实接口连通性用例 )"
dotnet test src/k-spider-test/k-spider-test.csproj --no-build --filter "TestCategory!=Live"

echo "==> verify ok"
