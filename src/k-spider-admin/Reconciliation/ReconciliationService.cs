using System.Text.Json;
using KSpider.Admin.Common.Json;
using KSpider.Admin.Common.Logging;
using KSpider.Admin.Repository.Abstractions;
using KSpider.Admin.Repository.Models;
using KSpider.Admin.Scheduler;
using KSpider.Admin.Tasks;
using KSpider.Admin.Tasks.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KSpider.Admin.Reconciliation;

/// <summary>
///     对账领域逻辑：只处理非终态任务，探测存活节点，重派或放弃。
/// </summary>
/// <remarks>
///     <para>
///         对账是调度循环的“修复器”：调度负责首次派发，对账负责发现卡住/失联/失败的任务，
///         并按探测结果决定保留、重派或放弃。所有状态写入仍经过 <see cref="IAdminTaskService" />，
///         避免对账和 API 各自实现一套状态机。
///     </para>
///     <para>
///         探测策略：只有 pushed / running 需要先探测；探测在单次超时内最多尝试
///         <c>ProbeAttempts</c> 次，全部失败才认为节点不可达，避免网络抖动造成双跑。
///     </para>
///     <para>
///         重派策略：不把任务派回刚刚失败的节点；没有其他在线 crawler 节点时保留任务，
///         等下一轮或人工处理。达到 <c>MaxFailCount</c> 后转 abandoned。
///     </para>
/// </remarks>
/// <param name="tasks">任务仓储；只读取非终态任务。</param>
/// <param name="adminTasks">admin 侧任务生命周期入口。</param>
/// <param name="probe">节点探测抽象。</param>
/// <param name="nodeDirectory">在线节点目录。</param>
/// <param name="dispatcher">下行派发抽象。</param>
/// <param name="roundGuard">同渠道互斥；重派后继续持有。</param>
/// <param name="nodeSelector">节点选择策略。</param>
/// <param name="options">对账参数。</param>
/// <param name="timeProvider">可测试时钟。</param>
/// <param name="logger">结构化日志。</param>
public sealed class ReconciliationService(
    ITaskRepository tasks,
    IAdminTaskService adminTasks,
    IProbeService probe,
    INodeDirectory nodeDirectory,
    INodeDispatcher dispatcher,
    ChannelRoundGuard roundGuard,
    NodeSelector nodeSelector,
    IOptions<ReconciliationOptions> options,
    TimeProvider timeProvider,
    ILogger<ReconciliationService> logger) : IReconciliationService
{
    private readonly ReconciliationOptions _options = options.Value;

    /// <summary>
    ///     扫描并处理超过无活动超时的非终态任务。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task ReconcileStaleTasksAsync(CancellationToken cancellationToken)
    {
        var before = timeProvider.GetUtcNow().UtcDateTime
            .AddSeconds(-_options.InactivityTimeoutSeconds);
        var staleTasks = await tasks.FindStaleAsync(before, _options.ScanBatchSize, cancellationToken);
        foreach (var task in staleTasks)
        {
            await ReconcileTaskAsync(task, cancellationToken);
        }
    }

    /// <summary>
    ///     立即重派某个失联节点名下的任务。
    /// </summary>
    /// <remarks>
    ///     节点心跳超时后由 <see cref="NodeHealthMonitor" /> 调用；不等待任务自然过期，
    ///     让故障恢复更快，同时避免把任务派回已判死的节点。
    /// </remarks>
    /// <param name="nodeId">已判死的节点标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task RequeueNodeTasksAsync(Guid nodeId, CancellationToken cancellationToken)
    {
        var nodeTasks = await tasks.FindNonTerminalByNodeAsync(nodeId, cancellationToken);
        foreach (var task in nodeTasks)
        {
            await RedispatchOrAbandonAsync(task, "节点判死", cancellationToken);
        }
    }

    private async Task ReconcileTaskAsync(SysTaskRecord task, CancellationToken cancellationToken)
    {
        if (!AdminTaskStatusExtensions.TryParse(task.Status, out var status) || status.IsTerminal())
        {
            return;
        }

        if (status is AdminTaskStatus.Pushed or AdminTaskStatus.Running)
        {
            var alive = await ProbeAsync(task, cancellationToken);
            if (alive)
            {
                // 节点仍存活：长任务可能只是还没回报进度，不在这里误杀。
                return;
            }

            AdminLog.ProbeFailed(logger, task.Id, task.NodeId);
        }

        await RedispatchOrAbandonAsync(task, "任务超时或节点不可达", cancellationToken);
    }

    private async Task RedispatchOrAbandonAsync(
        SysTaskRecord task,
        string reason,
        CancellationToken cancellationToken)
    {
        if (!AdminTaskStatusExtensions.TryParse(task.Status, out var status) || status.IsTerminal())
        {
            return;
        }

        var detail = TryReadDetail(task);
        if (detail is null)
        {
            AdminLog.TaskDetailMissing(logger, task.Id);
            return;
        }

        var extra = AdminJson.Deserialize<TaskExtraView>(task.ExtraJson);
        if (extra is null)
        {
            AdminLog.TaskDetailMissing(logger, task.Id);
            return;
        }

        if (extra.FailCount >= _options.MaxFailCount)
        {
            await adminTasks.TryAbandonAsync(task.Id, reason, cancellationToken);
            return;
        }

        // 探测失败/节点判死后不再回派同一节点；没有其他节点时留待下一轮或人工处理。
        var candidates = nodeDirectory.GetOnlineNodes(NodeTypes.Crawler)
            .Where(node => node.NodeId != task.NodeId)
            .ToList();
        var selected = nodeSelector.Select(candidates);
        if (selected is null)
        {
            AdminLog.ReconciliationNoNode(logger, task.Id, detail.ChannelId, reason);
            return;
        }

        var plan = new TaskPlan(detail.ChannelId, task.TaskType, detail);
        var result = await adminTasks.TryRedispatchAsync(
            task.Id,
            plan,
            selected.NodeId,
            _options.MaxFailCount,
            cancellationToken);

        if (result.Outcome == TaskTransitionOutcome.FailLimitReached)
        {
            await adminTasks.TryAbandonAsync(task.Id, reason, cancellationToken);
            return;
        }

        if (!result.Applied || result.Token is null)
        {
            AdminLog.ReconciliationNoNode(logger, task.Id, detail.ChannelId, "任务状态已变化或缺少新 token");
            return;
        }

        if (!roundGuard.TryAcquire(detail.ChannelId, task.Id))
        {
            AdminLog.ReconciliationNoNode(logger, task.Id, detail.ChannelId, "同渠道已有其他任务");
            return;
        }

        var request = new DispatchRequest(task.Id, plan.TaskType, plan.DetailJson(), result.Token.Value);
        var sent = await dispatcher.DispatchAsync(selected.NodeId, request, cancellationToken);
        if (sent)
        {
            await adminTasks.MarkPushedAsync(task.Id, result.Token.Value, cancellationToken);
            return;
        }

        AdminLog.DispatchFailed(logger, task.Id, selected.NodeId, "重派下行写入失败");
        await adminTasks.MarkPushFailedAsync(
            task.Id,
            result.Token.Value,
            "重派下行写入失败",
            cancellationToken);
    }

    /// <summary>
    ///     按 K/T 策略探测任务所在节点。
    /// </summary>
    /// <remarks>
    ///     单次探测使用独立超时，整体尝试次数与间隔来自配置；任一次存活即返回 true。
    ///     调用方退出时立即取消剩余重试，不拖住宿主关闭。
    /// </remarks>
    private async Task<bool> ProbeAsync(SysTaskRecord task, CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(
            Math.Max(1, _options.ProbeWindowSeconds / _options.ProbeAttempts));

        for (var attempt = 0; attempt < _options.ProbeAttempts; attempt++)
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(_options.ProbeTimeoutSeconds));
                var result = await probe.ProbeAsync(task.NodeId, task.Id, timeout.Token);
                if (result.Alive)
                {
                    return true;
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // 单次探测超时，进入下一次重试。
            }

            if (attempt + 1 < _options.ProbeAttempts)
            {
                await Task.Delay(delay, timeProvider, cancellationToken);
            }
        }

        return false;
    }

    private static TaskDetail? TryReadDetail(SysTaskRecord task)
    {
        try
        {
            return AdminJson.Deserialize<TaskDetail>(task.TaskDetailJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
