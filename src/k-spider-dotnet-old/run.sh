#!/usr/bin/env bash
# 部署启动脚本 : 与发布产物内携带的环境配置文件对齐 ( 发布时 -p:SpiderEnvironment=Production )
export DOTNET_ENVIRONMENT=${DOTNET_ENVIRONMENT:-Production}
nohup ./k-spider-dotnet > output.log 2>&1 &
