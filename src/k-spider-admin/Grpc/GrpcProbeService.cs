using System.Collections.Concurrent;
using KSpider.Admin.Tasks.Abstractions;
using Microsoft.Extensions.Options;

namespace KSpider.Admin.Grpc;

/// <summary>
///     基于 request_id 的探测请求/应答关联。
/// </summary>
/// <remarks>
///     <para>
///         每个探测生成唯一 request_id，发送前先登记等待者；节点回 probe_reply 时按 ID 唤醒。
///         超时、发送失败或取消都会清理等待项，避免字典无限增长。
///     </para>
///     <para>
///         探测只回答“节点连接是否仍可用”，不判断任务本身是否健康；任务超时与重派策略在
///         <see cref="Reconciliation.ReconciliationService" /> 中。
///     </para>
/// </remarks>
/// <param name="dispatcher">下行派发器。</param>
/// <param name="options">单次探测超时配置。</param>
public sealed class GrpcProbeService(
    INodeDispatcher dispatcher,
    IOptions<GrpcOptions> options) : IProbeService, IProbeReplySink
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<ProbeResult>> _pending = new();
    private readonly GrpcOptions _options = options.Value;

    /// <summary>
    ///     对指定节点执行一次探测。
    /// </summary>
    /// <param name="nodeId">目标节点。</param>
    /// <param name="taskId">待探测任务；用于日志与节点侧定位。</param>
    /// <param name="cancellationToken">调用方取消令牌。</param>
    /// <returns>节点应答存活为 true；发送失败、超时或取消为 false。</returns>
    public async Task<ProbeResult> ProbeAsync(
        Guid nodeId,
        long taskId,
        CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var completion = new TaskCompletionSource<ProbeResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        if (!_pending.TryAdd(requestId, completion))
        {
            return new ProbeResult(false);
        }

        try
        {
            var sent = await dispatcher.ProbeAsync(nodeId, requestId, taskId, cancellationToken);
            if (!sent)
            {
                return new ProbeResult(false);
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(_options.ProbeTimeoutSeconds));
            return await completion.Task.WaitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ProbeResult(false);
        }
        finally
        {
            _pending.TryRemove(requestId, out _);
        }
    }

    /// <summary>
    ///     节点 probe_reply 到达时唤醒对应等待者。
    /// </summary>
    /// <param name="requestId">探测关联 ID。</param>
    /// <param name="alive">节点自报是否存活。</param>
    public void Complete(string requestId, bool alive)
    {
        if (_pending.TryRemove(requestId, out var completion))
        {
            completion.TrySetResult(new ProbeResult(alive));
        }
    }
}
