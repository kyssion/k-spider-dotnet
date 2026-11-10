namespace KSpider.Admin.Tasks.Abstractions;

/// <summary>
///     一次任务下发请求。
/// </summary>
/// <param name="TaskId">任务主键。</param>
/// <param name="TaskType">任务类型。</param>
/// <param name="TaskDetailJson">任务包 JSON。</param>
/// <param name="DispatchToken">派发 token。</param>
public sealed record DispatchRequest(
    long TaskId,
    string TaskType,
    string TaskDetailJson,
    DispatchToken DispatchToken);

/// <summary>
///     一次取消请求。
/// </summary>
/// <param name="TaskId">任务主键。</param>
/// <param name="DispatchToken">取消时对应的派发 token。</param>
public sealed record CancelRequest(long TaskId, DispatchToken DispatchToken);

/// <summary>
///     下行写入抽象；Grpc 实现负责按连接串行化与超时。
/// </summary>
public interface INodeDispatcher
{
    /// <summary>
    ///     下发任务包。
    /// </summary>
    /// <param name="nodeId">目标节点。</param>
    /// <param name="request">任务包。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>写入成功返回 true。</returns>
    Task<bool> DispatchAsync(Guid nodeId, DispatchRequest request, CancellationToken cancellationToken);

    /// <summary>
    ///     发送取消快路径通知。
    /// </summary>
    /// <param name="nodeId">目标节点。</param>
    /// <param name="request">任务与 token。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>写入成功返回 true。</returns>
    Task<bool> CancelAsync(Guid nodeId, CancelRequest request, CancellationToken cancellationToken);

    /// <summary>
    ///     发送探测请求。
    /// </summary>
    /// <param name="nodeId">目标节点。</param>
    /// <param name="requestId">探测关联 ID。</param>
    /// <param name="taskId">待探测任务。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>写入成功返回 true。</returns>
    Task<bool> ProbeAsync(
        Guid nodeId,
        string requestId,
        long taskId,
        CancellationToken cancellationToken);
}
