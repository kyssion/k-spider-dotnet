# 0010 admin 采用模块化单体与后台循环

> 状态：已确认
> 最后更新：2026-11-10
> 决策人：项目所有者（执行指令）
> 关联：[admin.md](../10-architecture/admin.md)、[distributed.md](../10-architecture/distributed.md)、[auth.md](../10-architecture/auth.md)、[testing.md](../30-guides/testing.md)
> 影响：`src/k-spider-admin/` 目录结构、依赖方向、后台循环、REST/gRPC 端点与测试项目

## 背景

admin 是控制面，需要同时承担中央调度、对账、gRPC 节点服务、REST 管理 API、认证与前端托管。
当前只有 `Program.cs` 最小入口，需要先固定服务内部的模块边界，避免后续实现把调度、端点、
数据访问和传输逻辑混在一起。项目规模是一个服务、一个小团队，引入多层项目或重量级框架会增加
没有收益的仪式感。

## 备选项

| 选项 | 优点 | 代价 |
| --- | --- | --- |
| A. 单项目按模块分目录，接口约束依赖方向（选择） | 结构清晰、启动简单、符合「可读性优先」 | 需要靠评审与守卫维持边界，不能靠项目引用强制 |
| B. 拆成 Api / Application / Domain / Infrastructure 多项目 | 编译期强制分层 | 项目数量与依赖管理成本高，当前规模收益不足 |
| C. 所有逻辑放 `Program.cs` 与端点 | 起步最快 | 调度、认证、传输与数据访问互相缠绕，难以测试与演进 |

## 决策

选择 A。admin 保持一个 ASP.NET Core 项目，按 `Hosting / Scheduler / Reconciliation / Tasks /
Grpc / Api / Auth / WebHosting / Repository / Common` 分目录；模块之间通过接口依赖，由
`Hosting` 完成组合。调度与对账使用 `BackgroundService` 周期循环，不引入 Quartz 等第二套调度器；
REST 使用 Minimal API 端点组；gRPC 服务端使用 `Grpc.AspNetCore`；数据访问统一收敛到 `Repository`。
`Tasks/` 作为共享任务状态机模块，避免 Scheduler、Reconciliation、gRPC 与 API 各自实现状态迁移。

任务状态的事实源仍然是 PostgreSQL。节点内存连接表只负责在线判定与下行写入；`sys_task` 的状态
与进度由节点直写，admin 只负责推送系状态、取消、重派与放弃，并校验派发 token。

## 后果

- 正面：模块边界明确；调度、对账、认证和传输可以独立测试；后续拆分项目时目录已经是自然边界。
- 负面（需要接受的代价）：单项目内没有编译期模块隔离，必须靠接口、代码评审与测试守卫维持边界；
  新增 `Tasks/` 与 `Grpc.AspNetCore` 依赖需要在文档与依赖清单中登记。
- 需要同步修改的位置：`src/k-spider-admin/`、`k-spider-dotnet.sln`、`Directory.Packages.props`、
  [admin.md](../10-architecture/admin.md)、[testing.md](../30-guides/testing.md)、
  [dev-workflow.md](../30-guides/dev-workflow.md)、[roadmap.md](../00-overview/roadmap.md)。
- 触发重新评估的条件：admin 业务模块继续增长到单项目难以导航，或出现多个团队独立发布同一服务的需求。
