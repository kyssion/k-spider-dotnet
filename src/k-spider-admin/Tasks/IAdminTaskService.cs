using KSpider.Admin.Repository.Models;

namespace KSpider.Admin.Tasks;

/// <summary>
///     admin 侧任务生命周期入口。
/// </summary>
/// <remarks>
///     只暴露 admin 有权执行的状态迁移；running / success / run_failed 由节点直写，
///     admin 通过 <see cref="ObserveNodeResultAsync" /> 校验终态并释放互斥。
/// </remarks>
public interface IAdminTaskService
{
    /// <summary>
    ///     创建待派发任务包（pushing + 新 token）。
    /// </summary>
    /// <param name="plan">任务计划。</param>
    /// <param name="nodeId">目标执行节点。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<SysTaskRecord> CreateForDispatchAsync(
        TaskPlan plan,
        Guid nodeId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     条件更新 pushing → pushed。
    /// </summary>
    /// <param name="taskId">任务主键。</param>
    /// <param name="token">派发 token。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<TaskTransitionResult> MarkPushedAsync(
        long taskId,
        DispatchToken token,
        CancellationToken cancellationToken);

    /// <summary>
    ///     条件更新 pushing → push_failed。
    /// </summary>
    /// <param name="taskId">任务主键。</param>
    /// <param name="token">派发 token。</param>
    /// <param name="failure">失败原因。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<TaskTransitionResult> MarkPushFailedAsync(
        long taskId,
        DispatchToken token,
        string failure,
        CancellationToken cancellationToken);

    /// <summary>
    ///     取消非终态任务并尽力发送取消快路径通知。
    /// </summary>
    /// <param name="taskId">任务主键。</param>
    /// <param name="reason">取消原因。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<TaskTransitionResult> TryCancelAsync(
        long taskId,
        string reason,
        CancellationToken cancellationToken);

    /// <summary>
    ///     重派失败/卡住的任务，生成新 token。
    /// </summary>
    /// <param name="taskId">任务主键。</param>
    /// <param name="plan">新任务计划。</param>
    /// <param name="nodeId">新执行节点。</param>
    /// <param name="maxFailCount">最大失败次数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<TaskTransitionResult> TryRedispatchAsync(
        long taskId,
        TaskPlan plan,
        Guid nodeId,
        int maxFailCount,
        CancellationToken cancellationToken);

    /// <summary>
    ///     把任务转为 abandoned 终态。
    /// </summary>
    /// <param name="taskId">任务主键。</param>
    /// <param name="reason">放弃原因。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<TaskTransitionResult> TryAbandonAsync(
        long taskId,
        string reason,
        CancellationToken cancellationToken);

    /// <summary>
    ///     校验节点终态回报并释放互斥；不写终态。
    /// </summary>
    /// <param name="taskId">任务主键。</param>
    /// <param name="token">节点回报携带的 token。</param>
    /// <param name="nodeStatus">节点自报状态。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<TaskTransitionResult> ObserveNodeResultAsync(
        long taskId,
        DispatchToken token,
        AdminTaskStatus nodeStatus,
        CancellationToken cancellationToken);

    /// <summary>
    ///     读取任务当前记录。
    /// </summary>
    /// <param name="taskId">任务主键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<SysTaskRecord?> GetAsync(long taskId, CancellationToken cancellationToken);
}
