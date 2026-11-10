using System.Text.Json;
using Grpc.Core;
using KSpider.Admin.Common.Logging;
using KSpider.Admin.Repository.Abstractions;
using KSpider.Admin.Repository.Models;
using KSpider.Admin.Tasks;
using Microsoft.Extensions.Logging;
using GrpcScheduler = KSpider.Grpc.Scheduler;

namespace KSpider.Admin.Grpc;

/// <summary>
///     admin 的 gRPC 节点入口：注册、心跳、终态回报与探测应答。
/// </summary>
/// <remarks>
///     <para>
///         每条连接的生命周期：首条消息必须是 register → 校验节点身份 → 写 sys_node 在线镜像 →
///         回 register_ack → 持续读取 heartbeat / task_result / probe_reply。
///     </para>
///     <para>
///         在线判定的事实源是内存中的 <see cref="NodeConnectionRegistry" />；sys_node 只是镜像。
///         流断开时只把节点置为 offline 墓碑，不立即重派任务，避免短暂网络抖动导致双跑；
///         真正的判死与重派由心跳超时和对账负责。
///     </para>
///     <para>
///         同一 node_id 重连时新连接会顶替旧连接；旧连接的 finally 不能把新节点写成 offline。
///     </para>
/// </remarks>
/// <param name="registry">内存节点连接注册表。</param>
/// <param name="nodes">sys_node 镜像仓储。</param>
/// <param name="handler">节点上行消息处理器。</param>
/// <param name="logger">结构化日志。</param>
public sealed class SchedulerGrpcService(
    NodeConnectionRegistry registry,
    ISysNodeRepository nodes,
    NodeMessageHandler handler,
    ILogger<SchedulerGrpcService> logger) : GrpcScheduler.Scheduler.SchedulerBase
{
    /// <summary>
    ///     处理节点主动发起的 Connect 双向流。
    /// </summary>
    /// <param name="requestStream">节点上行消息流。</param>
    /// <param name="responseStream">admin 下行消息流。</param>
    /// <param name="context">gRPC 调用上下文；取消即节点断开。</param>
    public override async Task Connect(
        IAsyncStreamReader<GrpcScheduler.NodeMessage> requestStream,
        IServerStreamWriter<GrpcScheduler.AdminMessage> responseStream,
        ServerCallContext context)
    {
        var cancellationToken = context.CancellationToken;

        if (!await requestStream.MoveNext(cancellationToken))
        {
            return;
        }

        var first = requestStream.Current;
        if (first.PayloadCase != GrpcScheduler.NodeMessage.PayloadOneofCase.Register)
        {
            AdminLog.NodeRegisterRejected(logger, "首条消息不是 register");
            throw new RpcException(new Status(StatusCode.InvalidArgument, "首条消息必须是 register"));
        }

        var register = first.Register;
        if (!Guid.TryParse(register.NodeId, out var nodeId) || nodeId == Guid.Empty)
        {
            AdminLog.NodeRegisterRejected(logger, "node_id 不是合法 uuid");
            throw new RpcException(new Status(StatusCode.InvalidArgument, "node_id 必须是非空 uuid"));
        }

        if (!string.Equals(register.NodeType, NodeTypes.Crawler, StringComparison.Ordinal) &&
            !string.Equals(register.NodeType, NodeTypes.Process, StringComparison.Ordinal))
        {
            AdminLog.NodeRegisterRejected(logger, $"不支持的 node_type：{register.NodeType}");
            throw new RpcException(new Status(StatusCode.InvalidArgument, "node_type 只能是 crawler 或 process"));
        }

        var deviceJson = NormalizeDevice(register.DeviceJson);
        var connection = registry.Register(nodeId, register.NodeType, deviceJson, responseStream);

        try
        {
            await nodes.UpsertOnlineAsync(
                new SysNodeUpsert(nodeId, register.NodeType, null, null, deviceJson, "{}"),
                cancellationToken);

            AdminLog.NodeConnected(logger, nodeId, register.NodeType);

            var ack = new GrpcScheduler.AdminMessage
            {
                RegisterAck = new GrpcScheduler.RegisterAck { NodeId = register.NodeId },
            };

            if (!await connection.TrySendAsync(ack, cancellationToken))
            {
                return;
            }

            while (await requestStream.MoveNext(cancellationToken))
            {
                try
                {
                    await handler.HandleAsync(connection, requestStream.Current, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    // 单条消息处理失败不应断开健康节点；记日志后继续读流。
                    AdminLog.NodeMessageFailed(logger, exception, nodeId);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 节点正常断开。
        }
        catch (RpcException) when (cancellationToken.IsCancellationRequested)
        {
            // 节点正常断开。
        }
        finally
        {
            registry.Unregister(connection);
            AdminLog.NodeDisconnected(logger, nodeId);

            // 新连接可能已经顶替旧流；此时不能把新节点写成 offline。
            var hasNewerConnection =
                registry.TryGetConnection(nodeId, out var current) &&
                !ReferenceEquals(current, connection);
            if (!hasNewerConnection)
            {
                try
                {
                    // 清理路径使用独立取消令牌，避免节点断开后连 offline 墓碑都写不进去。
                    await nodes.MarkOfflineAsync(nodeId, CancellationToken.None);
                }
                catch (Exception exception)
                {
                    AdminLog.NodeOfflineWriteFailed(logger, exception, nodeId);
                }
            }
        }
    }

    /// <summary>
    ///     校验并归一化节点自报的 device JSON。
    /// </summary>
    /// <param name="deviceJson">节点上报的 JSON 字符串，可为空。</param>
    /// <returns>合法 JSON 原样返回；空值返回 null。</returns>
    /// <exception cref="RpcException">device_json 不是合法 JSON。</exception>
    private static string? NormalizeDevice(string? deviceJson)
    {
        if (string.IsNullOrWhiteSpace(deviceJson))
        {
            return null;
        }

        try
        {
            using var _ = JsonDocument.Parse(deviceJson);
            return deviceJson;
        }
        catch (JsonException)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "device_json 不是合法 JSON"));
        }
    }
}
