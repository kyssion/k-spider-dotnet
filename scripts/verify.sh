#!/usr/bin/env bash
# 一键验证 : 构建 + 全部单元测试 ( 离线 , 不依赖网络与数据库 )
set -euo pipefail
cd "$(dirname "$0")/.."

echo "==> dotnet build"
dotnet build k-spider-dotnet.sln

echo "==> dotnet test"
dotnet test src/k-spider-test/k-spider-test.csproj --no-build

echo "==> verify ok"
