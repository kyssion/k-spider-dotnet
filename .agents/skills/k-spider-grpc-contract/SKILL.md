---
name: k-spider-grpc-contract
description: >-
  修改 src/k-spider-grpc 的 proto、生成代码或契约测试时的流程与易漏项：
  草案状态、Grpc.Tools 生成方式、admin / crawler 引用、字段 / 错误码 / 流控待确认、
  契约测试与文档同步。任务涉及 scheduler.proto、TaskDispatch / TaskResult 或 gRPC 契约时使用。
---

# gRPC 契约变更

> 机制见 [distributed.md](../../../docs/10-architecture/distributed.md)「gRPC 双向流」；
> 项目决策见 [0008-grpc-contract-project.md](../../../docs/20-adr/0008-grpc-contract-project.md)
> 与 [0002-grpc-transport.md](../../../docs/20-adr/0002-grpc-transport.md)。
> 项目入口见 [k-spider-grpc/README.md](../../../src/k-spider-grpc/README.md)。

## 先确认

- proto 字段、错误码与流控仍是【待确认】；确认前不当作稳定契约。
- `task_detail` / `extra` 的 JSON 键名、类型与 casing 未定；不要自造字段。
- admin 与 crawler 只通过 ProjectReference 引用契约，不复制消息 DTO。

## 改 proto

1. 改 `src/k-spider-grpc/Protos/scheduler.proto`。
2. `dotnet build` 自动调用 Grpc.Tools 生成代码；不要手改 `obj/` 生成文件。
3. 补 `tests/k-spider-grpc-tests/` 的消息 round-trip；字段定稿后补字段 / 错误码 / 流控测试。
4. 同步 [distributed.md](../../../docs/10-architecture/distributed.md)、ADR 0008 与
   [roadmap.md](../../../docs/00-overview/roadmap.md)「M2」；契约确认前保留【待确认】。
5. 运行 `./scripts/verify.sh`。

## 易漏

- 只改 proto，不改契约测试与文档；
- 在 admin / crawler 里另写一份消息类型；
- 把草案字段写成既定事实；
- 忘记 admin / crawler 的 ProjectReference 或解决方案登记。
