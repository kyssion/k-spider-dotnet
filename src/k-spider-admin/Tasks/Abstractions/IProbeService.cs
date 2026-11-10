namespace KSpider.Admin.Tasks.Abstractions;

/// <summary>
///     一次探测结果。
/// </summary>
/// <param name="Alive">节点是否应答存活。</param>
public readonly record struct ProbeResult(bool Alive);

/// <summary>
///     对账探测抽象；Grpc 实现负责 request_id 关联与应答唤醒。
/// </summary>
public interface IProbeService
{
    /// <summary>
    ///     对任务所在节点执行一次探测。
    /// </summary>
    /// <param name="nodeId">目标节点。</param>
    /// <param name="taskId">待探测任务。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<ProbeResult> ProbeAsync(Guid nodeId, long taskId, CancellationToken cancellationToken);
}
