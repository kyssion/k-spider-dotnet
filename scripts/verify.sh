#!/usr/bin/env bash
# 一键验证 : 构建 + 全部单元测试 ( 离线 , 不依赖网络与数据库 )
set -euo pipefail
cd "$(dirname "$0")/.."

echo "==> 文档链接检查"
bash scripts/check-docs.sh

echo "==> dotnet build"
dotnet build k-spider-dotnet.sln

echo "==> dotnet test ( 离线 : 排除 Live 分类的真实接口连通性用例 )"
dotnet test src/k-spider-test/k-spider-test.csproj --no-build --filter "TestCategory!=Live"

echo "==> verify ok"
