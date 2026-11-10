using KSpider.Admin.Common.Json;
using KSpider.Admin.Common.Logging;
using KSpider.Admin.Repository.Abstractions;
using KSpider.Admin.Repository.Models;
using KSpider.Admin.Tasks.Abstractions;
using Microsoft.Extensions.Logging;

namespace KSpider.Admin.Tasks;

/// <summary>
///     admin 侧任务生命周期入口。
/// </summary>
/// <remarks>
///     <para>
///         职责边界：admin 只写“推送系”状态（pushing / pushed / push_failed）、取消、重派与放弃；
///         running / success / run_failed 由节点直接写数据库，admin 收到 task_result 时只校验 token
///         并释放同渠道互斥，绝不替节点写终态。
///     </para>
///     <para>
///         并发边界：所有状态迁移最终都落到 <see cref="ITaskRepository" /> 的条件更新；
///         本服务先读当前状态只用于给出可读的错误结果，真正的竞态由 SQL 条件与派发 token 兜底。
///         因此调用方必须把 <see cref="TaskTransitionOutcome.Conflict" /> 当作“状态已变化”，
///         而不是系统错误。
///     </para>
/// </remarks>
/// <param name="tasks">任务仓储；所有状态写入都必须经过它。</param>
/// <param name="dispatcher">下行派发抽象；取消时发送快路径通知，失败不改变已完成的取消结果。</param>
/// <param name="terminalObservers">终态观察者；调度器用它释放同渠道互斥。</param>
/// <param name="logger">结构化日志。</param>
public sealed class AdminTaskService(
    ITaskRepository tasks,
    INodeDispatcher dispatcher,
    IEnumerable<ITaskTerminalObserver> terminalObservers,
    ILogger<AdminTaskService> logger) : IAdminTaskService
{
    /// <summary>
    ///     创建一个待派发任务包。
    /// </summary>
    /// <remarks>
    ///     生成新的 <see cref="DispatchToken" /> 并写入 extra；插入后状态为 pushing。
    ///     调用方随后必须使用返回记录中的 token 派发，失败时调用 <see cref="MarkPushFailedAsync" />。
    /// </remarks>
    /// <param name="plan">任务计划；channel_id、batch_limit 必须有效。</param>
    /// <param name="nodeId">目标执行节点；sys_task.node_id 为 NOT NULL，因此必须先选节点。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>刚插入的任务记录。</returns>
    /// <exception cref="ArgumentException">TaskPlan 与 TaskDetail 的 channel_id 不一致。</exception>
    /// <exception cref="ArgumentOutOfRangeException">batch_limit 不是正数。</exception>
    public async Task<SysTaskRecord> CreateForDispatchAsync(
        TaskPlan plan,
        Guid nodeId,
        CancellationToken cancellationToken)
    {
        if (plan.Detail.ChannelId != plan.ChannelId)
        {
            throw new ArgumentException("TaskPlan 的 channel_id 与 TaskDetail 不一致", nameof(plan));
        }

        if (plan.Detail.BatchLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(plan), "batch_limit 必须大于 0");
        }

        var token = DispatchToken.New();
        var insert = new SysTaskInsert(
            nodeId,
            plan.TaskType,
            plan.DetailJson(),
            AdminTaskStatus.Pushing.ToDbValue(),
            AdminJson.Serialize(new TaskExtraWrite(token.Value, 0)));

        var task = await tasks.InsertAsync(insert, cancellationToken);
        AdminLog.TaskCreated(logger, task.Id, plan.ChannelId, nodeId, plan.TaskType, token.Value);

        return task;
    }

    /// <summary>
    ///     把任务从 pushing 标记为 pushed。
    /// </summary>
    /// <remarks>
    ///     节点可能在 admin 写 pushed 之前就收到 task_dispatch 并写入 running；
    ///     因此返回 <see cref="TaskTransitionOutcome.Conflict" /> 不一定代表失败，调用方应重新读取状态，
    ///     不要据此重派或放弃任务。
    /// </remarks>
    /// <param name="taskId">任务主键。</param>
    /// <param name="token">本次派发 token。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>状态迁移结果。</returns>
    public async Task<TaskTransitionResult> MarkPushedAsync(
        long taskId,
        DispatchToken token,
        CancellationToken cancellationToken)
    {
        var applied = await tasks.TryMarkPushedAsync(taskId, token.Value, cancellationToken);
        if (applied)
        {
            return new TaskTransitionResult(TaskTransitionOutcome.Applied);
        }

        return await ConflictAsync(taskId, cancellationToken);
    }

    /// <summary>
    ///     把任务从 pushing 标记为 push_failed。
    /// </summary>
    /// <param name="taskId">任务主键。</param>
    /// <param name="token">本次派发 token。</param>
    /// <param name="failure">失败原因；会截断后写入 extra.last_error。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>状态迁移结果。</returns>
    public async Task<TaskTransitionResult> MarkPushFailedAsync(
        long taskId,
        DispatchToken token,
        string failure,
        CancellationToken cancellationToken)
    {
        var applied = await tasks.TryMarkPushFailedAsync(
            taskId,
            token.Value,
            Truncate(failure),
            cancellationToken);
        if (applied)
        {
            return new TaskTransitionResult(TaskTransitionOutcome.Applied);
        }

        return await ConflictAsync(taskId, cancellationToken);
    }

    /// <summary>
    ///     取消一个非终态任务。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         先做数据库条件更新，再尽力发送 task_cancel 快路径通知。
    ///         通知失败不回滚取消：节点的终态写入会因状态已是 cancelled 或 token 不匹配而失败，
    ///         取消结果仍然收敛。
    ///     </para>
    ///     <para>
    ///         更新成功后立即通知终态观察者释放同渠道互斥；observer 使用独立于请求的取消令牌，
    ///         避免调用方断开连接时把互斥留死。
    ///     </para>
    /// </remarks>
    /// <param name="taskId">任务主键。</param>
    /// <param name="reason">取消原因；会截断后写入 extra.last_error。</param>
    /// <param name="cancellationToken">数据库操作的取消令牌。</param>
    /// <returns>状态迁移结果；已取消时幂等返回 Applied。</returns>
    public async Task<TaskTransitionResult> TryCancelAsync(
        long taskId,
        string reason,
        CancellationToken cancellationToken)
    {
        var task = await tasks.GetAsync(taskId, cancellationToken);
        if (task is null)
        {
            return new TaskTransitionResult(TaskTransitionOutcome.NotFound);
        }

        if (!AdminTaskStatusExtensions.TryParse(task.Status, out var current))
        {
            return new TaskTransitionResult(TaskTransitionOutcome.InvalidTransition, task);
        }

        if (current == AdminTaskStatus.Cancelled)
        {
            return new TaskTransitionResult(TaskTransitionOutcome.Applied, task);
        }

        if (!TaskStateMachine.CanTransition(current, AdminTaskStatus.Cancelled, TransitionActor.Admin))
        {
            return new TaskTransitionResult(TaskTransitionOutcome.InvalidTransition, task);
        }

        var applied = await tasks.TryCancelAsync(taskId, Truncate(reason), cancellationToken);
        if (!applied)
        {
            return await ConflictAsync(taskId, cancellationToken);
        }

        var channelId = ChannelIdOf(task);
        var extra = AdminJson.Deserialize<TaskExtraView>(task.ExtraJson);
        if (extra?.DispatchToken is not null)
        {
            try
            {
                await dispatcher.CancelAsync(
                    task.NodeId,
                    new CancelRequest(taskId, DispatchToken.From(extra.DispatchToken)),
                    CancellationToken.None);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                AdminLog.TaskCancelSendFailed(logger, taskId, task.NodeId);
            }
        }

        await NotifyTerminalAsync(task);
        AdminLog.TaskTransitionApplied(logger, taskId, channelId, "cancelled", reason);
        return new TaskTransitionResult(TaskTransitionOutcome.Applied, task);
    }

    /// <summary>
    ///     把失败或被卡住的任务重派到指定节点。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         每次重派都会生成新 token 并让仓储把 fail_count 加一；旧节点的迟到终态会因 token
    ///         不匹配而无法覆盖新进度。
    ///     </para>
    ///     <para>
    ///         达到 <paramref name="maxFailCount" /> 时返回
    ///         <see cref="TaskTransitionOutcome.FailLimitReached" />，由调用方决定转为 abandoned。
    ///         本方法不直接放弃任务，避免策略散落在多个模块。
    ///     </para>
    /// </remarks>
    /// <param name="taskId">任务主键。</param>
    /// <param name="plan">新的任务计划；用于更新 task_detail。</param>
    /// <param name="nodeId">新执行节点。</param>
    /// <param name="maxFailCount">允许的最大失败次数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>状态迁移结果；成功时携带新 token。</returns>
    public async Task<TaskTransitionResult> TryRedispatchAsync(
        long taskId,
        TaskPlan plan,
        Guid nodeId,
        int maxFailCount,
        CancellationToken cancellationToken)
    {
        var task = await tasks.GetAsync(taskId, cancellationToken);
        if (task is null)
        {
            return new TaskTransitionResult(TaskTransitionOutcome.NotFound);
        }

        if (!AdminTaskStatusExtensions.TryParse(task.Status, out var current))
        {
            return new TaskTransitionResult(TaskTransitionOutcome.InvalidTransition, task);
        }

        if (!TaskStateMachine.CanTransition(current, AdminTaskStatus.Pushing, TransitionActor.Admin))
        {
            return new TaskTransitionResult(TaskTransitionOutcome.InvalidTransition, task);
        }

        var extra = AdminJson.Deserialize<TaskExtraView>(task.ExtraJson);
        if (extra is null || extra.FailCount >= maxFailCount)
        {
            return new TaskTransitionResult(TaskTransitionOutcome.FailLimitReached, task);
        }

        var token = DispatchToken.New();
        var applied = await tasks.TryRedispatchAsync(
            taskId,
            nodeId,
            plan.DetailJson(),
            token.Value,
            cancellationToken);
        if (!applied)
        {
            return await ConflictAsync(taskId, cancellationToken);
        }

        AdminLog.ReconciliationRedispatch(
            logger,
            taskId,
            plan.ChannelId,
            nodeId,
            extra.FailCount + 1,
            token.Value);
        return new TaskTransitionResult(TaskTransitionOutcome.Applied, task, token);
    }

    /// <summary>
    ///     把任务转为 abandoned 终态。
    /// </summary>
    /// <param name="taskId">任务主键。</param>
    /// <param name="reason">放弃原因；会截断后写入 extra.last_error。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>状态迁移结果；已放弃时幂等返回 Applied。</returns>
    public async Task<TaskTransitionResult> TryAbandonAsync(
        long taskId,
        string reason,
        CancellationToken cancellationToken)
    {
        var task = await tasks.GetAsync(taskId, cancellationToken);
        if (task is null)
        {
            return new TaskTransitionResult(TaskTransitionOutcome.NotFound);
        }

        if (!AdminTaskStatusExtensions.TryParse(task.Status, out var current))
        {
            return new TaskTransitionResult(TaskTransitionOutcome.InvalidTransition, task);
        }

        if (current == AdminTaskStatus.Abandoned)
        {
            return new TaskTransitionResult(TaskTransitionOutcome.Applied, task);
        }

        if (!TaskStateMachine.CanTransition(current, AdminTaskStatus.Abandoned, TransitionActor.Admin))
        {
            return new TaskTransitionResult(TaskTransitionOutcome.InvalidTransition, task);
        }

        var applied = await tasks.TryAbandonAsync(taskId, Truncate(reason), cancellationToken);
        if (!applied)
        {
            return await ConflictAsync(taskId, cancellationToken);
        }

        var channelId = ChannelIdOf(task);
        await NotifyTerminalAsync(task);
        AdminLog.ReconciliationAbandoned(logger, taskId, channelId, reason);
        return new TaskTransitionResult(TaskTransitionOutcome.Applied, task);
    }

    /// <summary>
    ///     处理节点上报的终态消息。
    /// </summary>
    /// <remarks>
    ///     节点已经直接写数据库；本方法只做两件事：校验 token 与数据库状态，然后在确认终态后
    ///     通知观察者释放互斥。若数据库尚未落到终态（例如节点写库失败），返回 Conflict，
    ///     由对账扫描兜底，admin 不在这里补写终态。
    /// </remarks>
    /// <param name="taskId">任务主键。</param>
    /// <param name="token">节点回报携带的派发 token。</param>
    /// <param name="nodeStatus">节点自报状态；仅用于日志与一致性诊断。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>状态迁移结果。</returns>
    public async Task<TaskTransitionResult> ObserveNodeResultAsync(
        long taskId,
        DispatchToken token,
        AdminTaskStatus nodeStatus,
        CancellationToken cancellationToken)
    {
        var task = await tasks.GetAsync(taskId, cancellationToken);
        if (task is null)
        {
            return new TaskTransitionResult(TaskTransitionOutcome.NotFound);
        }

        var extra = AdminJson.Deserialize<TaskExtraView>(task.ExtraJson);
        if (extra?.DispatchToken is null ||
            !string.Equals(extra.DispatchToken, token.Value, StringComparison.Ordinal))
        {
            AdminLog.TaskStaleResult(logger, taskId, token.Value);
            return new TaskTransitionResult(TaskTransitionOutcome.Conflict, task);
        }

        if (!AdminTaskStatusExtensions.TryParse(task.Status, out var current) || !current.IsTerminal())
        {
            // 节点回报终态但数据库尚未落地：交给对账重试，admin 不替节点写终态。
            AdminLog.TaskResultMismatch(logger, taskId, nodeStatus.ToDbValue(), task.Status);
            return new TaskTransitionResult(TaskTransitionOutcome.Conflict, task);
        }

        await NotifyTerminalAsync(task);
        return new TaskTransitionResult(TaskTransitionOutcome.Applied, task);
    }

    /// <summary>
    ///     读取任务当前记录。
    /// </summary>
    /// <param name="taskId">任务主键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>任务记录；不存在时返回 null。</returns>
    public Task<SysTaskRecord?> GetAsync(long taskId, CancellationToken cancellationToken)
    {
        return tasks.GetAsync(taskId, cancellationToken);
    }

    private async Task<TaskTransitionResult> ConflictAsync(long taskId, CancellationToken cancellationToken)
    {
        var current = await tasks.GetAsync(taskId, cancellationToken);
        return current is null
            ? new TaskTransitionResult(TaskTransitionOutcome.NotFound)
            : new TaskTransitionResult(TaskTransitionOutcome.Conflict, current);
    }

    private async Task NotifyTerminalAsync(SysTaskRecord task)
    {
        var detail = TryReadDetail(task);
        if (detail is null)
        {
            AdminLog.TaskDetailMissing(logger, task.Id);
            return;
        }

        foreach (var observer in terminalObservers)
        {
            try
            {
                // 互斥释放属于本地快速操作，不跟随 HTTP/gRPC 请求取消，避免请求断开后留下死锁。
                await observer.OnTaskTerminalAsync(task.Id, detail.ChannelId, CancellationToken.None);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                AdminLog.TaskObserverFailed(
                    logger,
                    exception,
                    task.Id,
                    detail.ChannelId,
                    observer.GetType().Name);
            }
        }
    }

    private static TaskDetail? TryReadDetail(SysTaskRecord task)
    {
        try
        {
            return AdminJson.Deserialize<TaskDetail>(task.TaskDetailJson);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private static long ChannelIdOf(SysTaskRecord task)
    {
        return TryReadDetail(task)?.ChannelId ?? 0;
    }

    private static string Truncate(string value)
    {
        const int maxLength = 2000;
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    /// <summary>
    ///     创建任务时写入 extra 的最小结构；后续由仓储在 SQL 中增量合并。
    /// </summary>
    private sealed record TaskExtraWrite(string DispatchToken, int FailCount);
}
