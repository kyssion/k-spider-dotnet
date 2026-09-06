#!/usr/bin/env bash
# 部署启动脚本 : 与发布产物内携带的环境配置文件对齐 ( 发布时 -p:SpiderEnvironment=prod )
export DOTNET_ENVIRONMENT=${DOTNET_ENVIRONMENT:-prod}
nohup ./k-spider-dotnet > output.log 2>&1 &
