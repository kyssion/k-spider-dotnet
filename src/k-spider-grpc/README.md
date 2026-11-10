# k-spider-grpc

admin 与 crawler 共享的 gRPC 契约项目：`Protos/scheduler.proto` + Grpc.Tools 生成代码。

> 草案状态：proto 字段、错误码与流控仍待确认，确认前不视为稳定契约。
> 机制与状态机见 [distributed.md](../../docs/10-architecture/distributed.md)，项目决策见
> [0008-grpc-contract-project.md](../../docs/20-adr/0008-grpc-contract-project.md)。

## 使用方式

- admin / crawler 通过 ProjectReference 引用；不要复制 proto 或手写消息 DTO。
- `dotnet build` 自动调用 Grpc.Tools 生成代码，生成文件在 `obj/`，不要手改。
- 契约测试在 `tests/k-spider-grpc-tests/`；改 proto 后补消息 round-trip。

## 变更流程

1. 先确认对应待确认项（proto 字段、错误码、流控）。
2. 修改 `Protos/scheduler.proto`，重新构建生成代码。
3. 补 `tests/k-spider-grpc-tests/` 的 round-trip。
4. 同步 [distributed.md](../../docs/10-architecture/distributed.md)、[roadmap.md](../../docs/00-overview/roadmap.md)
   与相关待确认项，运行 `./scripts/verify.sh`。
