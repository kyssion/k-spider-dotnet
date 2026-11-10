# 0008 gRPC 契约项目采用共享生成库

> 状态：已确认（待办为历史快照，进度见 roadmap）
> 最后更新：2026-11-09
> 决策人：项目所有者（执行指令）
> 关联：[0002-grpc-transport.md](0002-grpc-transport.md)、[distributed.md](../10-architecture/distributed.md)、[roadmap.md](../00-overview/roadmap.md)
> 影响：`src/k-spider-grpc/`、admin / crawler 项目引用、proto 生成方式、NuGet 依赖

## 背景

M2 需要 admin 与 crawler 共享 `Connect` 双向流的消息类型与服务契约。如果各服务各自维护 proto 或手写 DTO，
字段会漂移；如果把契约放进 admin 或 crawler，会形成服务间项目引用，违反「各服务不共享实体项目」的边界。

## 备选项

| 选项 | 优点 | 代价 |
| --- | --- | --- |
| A. 各服务各自维护 proto / 生成代码 | 服务独立 | 重复与漂移；联调成本高 |
| B. 共享契约项目 `src/k-spider-grpc/`，`Grpc.Tools` 生成 Both（选择） | 单一 proto；admin / crawler 同引用；生成代码一致 | 契约变更影响两个服务；新增生成工具依赖 |
| C. 手写消息 DTO + 共享接口 | 不依赖 proto 工具 | 序列化与流式契约手写易错；失去 proto 生态 |

## 决策

选择 B。`src/k-spider-grpc/` 存放 `Protos/scheduler.proto` 与生成代码，`GrpcServices="Both"`；
admin / crawler 通过 ProjectReference 引用。proto 字段当前是草案，确认前不视为稳定契约。

## 后果

- 正面：单一契约源；生成代码一致；服务之间不共享实体项目。
- 负面（需要接受的代价）：新增 `Google.Protobuf` / `Grpc.Core.Api` / `Grpc.Tools` 依赖；
  proto 变更需要同时重建两个服务；生成代码与手写代码的边界需要评审。
- 需要同步修改的位置：`Directory.Packages.props`、`src/k-spider-grpc/`、admin / crawler 的 csproj、
  [distributed.md](../10-architecture/distributed.md)、[roadmap.md](../00-overview/roadmap.md)。
- 触发重新评估的条件：出现第三个消费者且需要独立版本；或 proto 生成方式与构建性能发生冲突。

## 待办

- [x] 创建共享契约项目与生成代码
- [x] admin / crawler 引用
- [x] 契约消息 round-trip 测试
- [ ] 确认 proto 字段、错误码与流控
