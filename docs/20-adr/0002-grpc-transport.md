# 0002 gRPC 传输与端口拆分

> 状态：已确认
> 最后更新：2026-11-04
> 关联：[distributed.md](../10-architecture/distributed.md)
> 影响：admin 监听配置、节点连接、部署端口

## 背景

admin 同时提供 REST 管理 API 与 gRPC 节点流。最初考虑用单端口 Kestrel `Http1AndHttp2` 加 h2c 明文同时承载两者，但明文下无法可靠协商 HTTP/2 gRPC。

## 决策

REST 与 gRPC 使用分离的监听端点：REST 走 HTTP/1.1，gRPC 端点只承载 HTTP/2；内网 gRPC 默认 h2c 明文，TLS 按端点预留。节点主动连接 admin，不监听入站端口。

## 影响与取舍

- 好处：协议行为确定，避免单端口明文 `Http1AndHttp2` 的协商问题；
- 代价：部署需要开放两个端口，配置项增加；
- 节点认证、proto 字段和 TLS 细节仍是待确认项，见 [20-adr/README.md](README.md)。
