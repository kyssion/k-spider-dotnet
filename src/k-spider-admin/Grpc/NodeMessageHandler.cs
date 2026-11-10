using System.Globalization;
using KSpider.Admin.Common.Logging;
using KSpider.Admin.Tasks;
using Microsoft.Extensions.Logging;
using GrpcScheduler = KSpider.Grpc.Scheduler;

namespace KSpider.Admin.Grpc;

/// <summary>
///     处理节点上行消息：心跳、任务终态、探测应答。
/// </summary>
/// <remarks>
///     本处理器不直接写数据库；终态校验与互斥释放交给 <see cref="IAdminTaskService" />。
///     单条消息失败由调用方记录后继续读流，避免一个坏消息拖垮节点连接。
/// </remarks>
/// <param name="adminTasks">admin 侧任务生命周期入口。</param>
/// <param name="probeReplies">探测应答通道；按 request_id 唤醒等待者。</param>
/// <param name="logger">结构化日志。</param>
public sealed class NodeMessageHandler(
    IAdminTaskService adminTasks,
    IProbeReplySink probeReplies,
    ILogger<NodeMessageHandler> logger)
{
    /// <summary>
    ///     分发一条节点消息。
    /// </summary>
    /// <param name="connection">消息所属连接；心跳用它更新内存快照。</param>
    /// <param name="message">节点上行消息。</param>
    /// <param name="cancellationToken">流取消令牌。</param>
    public async Task HandleAsync(
        NodeConnection connection,
        GrpcScheduler.NodeMessage message,
        CancellationToken cancellationToken)
    {
        switch (message.PayloadCase)
        {
            case GrpcScheduler.NodeMessage.PayloadOneofCase.Heartbeat:
                connection.Touch(message.Heartbeat.RunningTasks);
                break;
            case GrpcScheduler.NodeMessage.PayloadOneofCase.TaskResult:
                await HandleTaskResultAsync(message.TaskResult, cancellationToken);
                break;
            case GrpcScheduler.NodeMessage.PayloadOneofCase.ProbeReply:
                probeReplies.Complete(message.ProbeReply.RequestId, message.ProbeReply.Alive);
                break;
            case GrpcScheduler.NodeMessage.PayloadOneofCase.Register:
                // 注册只接受首条消息；后续重复注册忽略。
                break;
            case GrpcScheduler.NodeMessage.PayloadOneofCase.None:
            default:
                break;
        }
    }

    /// <summary>
    ///     处理节点终态回报。
    /// </summary>
    /// <remarks>
    ///     节点已经写入数据库；这里只解析并转交 <see cref="IAdminTaskService" /> 做 token 校验。
    ///     状态字符串或 token 缺失时只记日志，不尝试修正数据库。
    /// </remarks>
    private async Task HandleTaskResultAsync(
        GrpcScheduler.TaskResult result,
        CancellationToken cancellationToken)
    {
        if (!long.TryParse(result.TaskId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var taskId) ||
            !AdminTaskStatusExtensions.TryParse(result.Status, out var status) ||
            string.IsNullOrWhiteSpace(result.DispatchToken))
        {
            AdminLog.NodeResultInvalid(logger, result.TaskId, result.Status);
            return;
        }

        await adminTasks.ObserveNodeResultAsync(
            taskId,
            DispatchToken.From(result.DispatchToken),
            status,
            cancellationToken);
    }
}
