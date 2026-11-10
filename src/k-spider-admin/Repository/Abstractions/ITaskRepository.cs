using KSpider.Admin.Repository.Models;

namespace KSpider.Admin.Repository.Abstractions;

/// <summary>
///     sys_task 数据访问边界。
/// </summary>
/// <remarks>
///     <para>
///         所有“尝试迁移”方法都是条件更新：只有当前状态与 token 满足条件才返回 true。
///         返回 false 表示状态已变化（取消/重派/节点已推进），调用方应按 Conflict 处理。
///     </para>
///     <para>
///         运行系状态与终态由节点直写；本接口的写入方法只服务 admin 侧生命周期。
///     </para>
/// </remarks>
public interface ITaskRepository
{
    /// <summary>
    ///     插入一条 pushing 任务并返回完整记录。
    /// </summary>
    /// <param name="insert">节点、任务类型、task_detail、状态与初始 extra。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<SysTaskRecord> InsertAsync(SysTaskInsert insert, CancellationToken cancellationToken);

    /// <summary>
    ///     按主键读取任务。
    /// </summary>
    /// <param name="id">任务主键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>任务记录；不存在时返回 null。</returns>
    Task<SysTaskRecord?> GetAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    ///     读取全部非终态任务；启动时用于恢复同渠道互斥。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<IReadOnlyList<SysTaskRecord>> FindNonTerminalAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     读取指定节点名下的非终态任务；节点判死后立即重派。
    /// </summary>
    /// <param name="nodeId">节点标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<IReadOnlyList<SysTaskRecord>> FindNonTerminalByNodeAsync(
        Guid nodeId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     读取超过无活动超时的非终态任务。
    /// </summary>
    /// <param name="beforeUtc">update_time 上限（UTC）。</param>
    /// <param name="limit">单批上限。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<IReadOnlyList<SysTaskRecord>> FindStaleAsync(
        DateTime beforeUtc,
        int limit,
        CancellationToken cancellationToken);

    /// <summary>
    ///     读取每个渠道最近一次任务创建时间；用于恢复调度到期计算。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<IReadOnlyDictionary<long, DateTime>> GetLastRunTimesAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     条件更新 pushing → pushed。
    /// </summary>
    /// <param name="id">任务主键。</param>
    /// <param name="token">派发 token；必须与 extra 中的值一致。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>更新成功返回 true。</returns>
    Task<bool> TryMarkPushedAsync(long id, string token, CancellationToken cancellationToken);

    /// <summary>
    ///     条件更新 pushing → push_failed，并记录失败原因。
    /// </summary>
    /// <param name="id">任务主键。</param>
    /// <param name="token">派发 token。</param>
    /// <param name="failure">失败原因。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>更新成功返回 true。</returns>
    Task<bool> TryMarkPushFailedAsync(
        long id,
        string token,
        string failure,
        CancellationToken cancellationToken);

    /// <summary>
    ///     条件更新非终态 → cancelled。
    /// </summary>
    /// <param name="id">任务主键。</param>
    /// <param name="reason">取消原因。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>更新成功返回 true。</returns>
    Task<bool> TryCancelAsync(long id, string reason, CancellationToken cancellationToken);

    /// <summary>
    ///     条件重派：更新节点、task_detail、新 token，并把 fail_count 加一。
    /// </summary>
    /// <param name="id">任务主键。</param>
    /// <param name="nodeId">新执行节点。</param>
    /// <param name="taskDetailJson">新任务包 JSON。</param>
    /// <param name="token">新派发 token。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>更新成功返回 true。</returns>
    Task<bool> TryRedispatchAsync(
        long id,
        Guid nodeId,
        string taskDetailJson,
        string token,
        CancellationToken cancellationToken);

    /// <summary>
    ///     条件更新非终态 → abandoned，并记录原因。
    /// </summary>
    /// <param name="id">任务主键。</param>
    /// <param name="reason">放弃原因。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>更新成功返回 true。</returns>
    Task<bool> TryAbandonAsync(long id, string reason, CancellationToken cancellationToken);

    /// <summary>
    ///     分批删除超过保留期的终态任务；非终态行永不删除。
    /// </summary>
    /// <param name="cutoffUtc">update_time 截止时间（UTC）。</param>
    /// <param name="batchSize">单批上限。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>实际删除行数。</returns>
    Task<int> DeleteTerminalOlderThanAsync(
        DateTime cutoffUtc,
        int batchSize,
        CancellationToken cancellationToken);

    /// <summary>
    ///     分页查询任务。
    /// </summary>
    /// <param name="query">分页、状态与节点筛选。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<PageResult<SysTaskRecord>> QueryAsync(TaskQuery query, CancellationToken cancellationToken);
}
