namespace KSpider.Admin.Reconciliation;

/// <summary>
///     对账领域服务。
/// </summary>
public interface IReconciliationService
{
    /// <summary>
    ///     扫描并处理超过无活动超时的非终态任务。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    Task ReconcileStaleTasksAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     立即重派某个失联节点名下的任务。
    /// </summary>
    /// <param name="nodeId">已判死的节点标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task RequeueNodeTasksAsync(Guid nodeId, CancellationToken cancellationToken);
}
