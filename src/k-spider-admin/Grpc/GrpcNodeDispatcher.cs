using System.Globalization;
using KSpider.Admin.Common.Logging;
using KSpider.Admin.Tasks.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GrpcScheduler = KSpider.Grpc.Scheduler;

namespace KSpider.Admin.Grpc;

/// <summary>
///     基于 gRPC 双向流的节点下行派发器。
/// </summary>
/// <remarks>
///     每条连接写操作由 <see cref="NodeConnection" /> 串行化；这里额外加发送超时，
///     避免网络半死时调度/对账线程被写操作卡住。发送失败只返回 false，不抛业务异常，
///     调用方据此把任务置为 push_failed 并交给对账重试。
/// </remarks>
/// <param name="registry">节点连接注册表。</param>
/// <param name="options">gRPC 发送超时配置。</param>
/// <param name="logger">结构化日志。</param>
public sealed class GrpcNodeDispatcher(
    NodeConnectionRegistry registry,
    IOptions<GrpcOptions> options,
    ILogger<GrpcNodeDispatcher> logger) : INodeDispatcher
{
    private readonly GrpcOptions _options = options.Value;

    /// <summary>
    ///     下发任务包。
    /// </summary>
    /// <param name="nodeId">目标节点。</param>
    /// <param name="request">任务包内容与派发 token。</param>
    /// <param name="cancellationToken">调用方取消令牌。</param>
    /// <returns>写入成功返回 true；节点不在线、超时或流写入失败返回 false。</returns>
    public Task<bool> DispatchAsync(
        Guid nodeId,
        DispatchRequest request,
        CancellationToken cancellationToken)
    {
        var message = new GrpcScheduler.AdminMessage
        {
            TaskDispatch = new GrpcScheduler.TaskDispatch
            {
                TaskId = request.TaskId.ToString(CultureInfo.InvariantCulture),
                TaskType = request.TaskType,
                TaskDetailJson = request.TaskDetailJson,
                DispatchToken = request.DispatchToken.Value,
            },
        };

        return SendAsync(nodeId, message, "task_dispatch", cancellationToken);
    }

    /// <summary>
    ///     发送取消快路径通知。
    /// </summary>
    /// <param name="nodeId">目标节点。</param>
    /// <param name="request">任务与派发 token。</param>
    /// <param name="cancellationToken">调用方取消令牌。</param>
    /// <returns>写入成功返回 true；失败返回 false。</returns>
    public Task<bool> CancelAsync(Guid nodeId, CancelRequest request, CancellationToken cancellationToken)
    {
        var message = new GrpcScheduler.AdminMessage
        {
            TaskCancel = new GrpcScheduler.TaskCancel
            {
                TaskId = request.TaskId.ToString(CultureInfo.InvariantCulture),
                DispatchToken = request.DispatchToken.Value,
            },
        };

        return SendAsync(nodeId, message, "task_cancel", cancellationToken);
    }

    /// <summary>
    ///     发送单次探测请求。
    /// </summary>
    /// <param name="nodeId">目标节点。</param>
    /// <param name="requestId">探测关联 ID。</param>
    /// <param name="taskId">待探测任务。</param>
    /// <param name="cancellationToken">调用方取消令牌。</param>
    /// <returns>写入成功返回 true；失败返回 false。</returns>
    public Task<bool> ProbeAsync(
        Guid nodeId,
        string requestId,
        long taskId,
        CancellationToken cancellationToken)
    {
        var message = new GrpcScheduler.AdminMessage
        {
            ProbeRequest = new GrpcScheduler.ProbeRequest
            {
                RequestId = requestId,
                TaskId = taskId.ToString(CultureInfo.InvariantCulture),
            },
        };

        return SendAsync(nodeId, message, "probe_request", cancellationToken);
    }

    private async Task<bool> SendAsync(
        Guid nodeId,
        GrpcScheduler.AdminMessage message,
        string messageType,
        CancellationToken cancellationToken)
    {
        if (!registry.TryGetConnection(nodeId, out var connection))
        {
            // 节点不在线不是异常：调用方会把任务置为 push_failed 或等待下一轮对账。
            return false;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.SendTimeoutSeconds));

        try
        {
            var sent = await connection.TrySendAsync(message, timeout.Token);
            if (!sent)
            {
                AdminLog.GrpcSendFailed(logger, nodeId, messageType);
            }

            return sent;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // 发送超时：连接可能只是半死，交给上层重派，不在传输层重试。
            AdminLog.GrpcSendFailed(logger, nodeId, messageType);
            return false;
        }
    }
}
