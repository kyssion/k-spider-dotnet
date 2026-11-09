# 安全策略

> 本文只登记报告渠道、支持范围与已知限制；行为规则以对应权威文档为准。

## 报告漏洞

**请不要在公开 issue 中披露漏洞细节。**

优先使用 GitHub 的私有漏洞报告：打开仓库 **Security** 标签页 → **Report a vulnerability**，
填写复现步骤、影响范围与判断依据。若入口不可见（尚未启用），开一个 issue **仅说明「希望获得私下沟通渠道」**，
不要在公开线程写细节。

## 支持范围与响应

- 当前维护 v2 代码与契约（`docs/`、`db/`、`src/k-spider-*` 中的现行服务）；
- `src/k-spider-dotnet-old/` 是 v1 历史归档，不承诺安全修复；`src/k-spider-test/` 的夹具不含真实凭据或个人数据；
- 个人维护项目，不承诺 SLA；会尽快确认收到，并在确认问题后给出处理计划或「不修 + 理由」；
- 请提供可复现证据（命令 + 输出，或最小复现步骤），没有复现路径的报告通常无法推进。

## 什么算安全问题

- 依赖组件中已知但未被本项目流程覆盖的漏洞（NuGet / npm）；
- 抓取 / 文件处理引入后的 SSRF、路径穿越、命令注入；
- 密钥、Cookie、Token 进入仓库、日志、文档或测试夹具；
- 认证 / 授权缺陷（在认证链路落地之后）；
- 供应链与 CI：GitHub Actions 权限过宽、依赖混淆。

## 什么不算安全问题（已知限制）

| 现象 | 说明 |
| --- | --- |
| 认证与账号尚未实现 | 只有设计见 [auth.md](docs/10-architecture/auth.md)；实现前不要把服务暴露到不可信网络 |
| 初始化账号 | 账号与密码规则见 [auth.md](docs/10-architecture/auth.md)「默认账号」；部署后必须修改初始密码 |
| gRPC 内网 h2c 明文、节点认证未定 | 见 [distributed.md](docs/10-architecture/distributed.md)「监听端点」；生产部署前必须先关闭该待确认项 |
| 前端依赖审计尚未接入 CI | 后端 NuGet 审计随 restore / build 输出；pnpm 审计在 CI 接入前由人工核对安全公告 |
| v1 归档代码 | 不作现行依据，不承诺安全修复 |

## 安全相关权威文档

报告前先确认相关设计是否属于已登记限制；条款本身不在本文复制。

| 主题 | 唯一来源 |
| --- | --- |
| 认证、会话、CSRF 与账号管理 | [auth.md](docs/10-architecture/auth.md) |
| 节点认证、gRPC 信任边界与端口拆分 | [distributed.md](docs/10-architecture/distributed.md) |
| 配置优先级、凭据注入与凭据红线 | [AGENTS.md](AGENTS.md) |
| 数据模型、DDL 与写入契约 | [data-model.md](docs/10-architecture/data-model.md) |
| 依赖漏洞升级与记录流程 | 本文「凭据与依赖」与 [change-protocol.md](docs/50-ai/change-protocol.md) |

## 凭据与依赖

- 仓库不提交真实凭据；连接串、密钥与节点认证材料由部署方通过配置/环境变量注入，见
  [AGENTS.md](AGENTS.md) 的配置优先级与凭据红线；
- 新增依赖必须先在 [20-adr/README.md](docs/20-adr/README.md) 登记并确认，不使用来源不明的包；
- 发现凭据误提交时，先轮换凭据，再清理历史；仅删除文件不算修复。
