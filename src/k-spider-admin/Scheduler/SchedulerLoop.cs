using System.Text.Json;
using KSpider.Admin.Common.Background;
using KSpider.Admin.Common.Json;
using KSpider.Admin.Common.Logging;
using KSpider.Admin.Repository.Abstractions;
using KSpider.Admin.Tasks;
using KSpider.Admin.Tasks.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KSpider.Admin.Scheduler;

/// <summary>
///     中央调度循环：读 active 渠道 → 判断到期 → 选节点 → 建任务包 → 派发。
/// </summary>
/// <remarks>
///     <para>
///         单轮流程是“渠道级”的：同一渠道在 guard 持有时直接跳过，不会因为上一轮未结束而
///         重复建任务。首次运行时先从 sys_task 恢复最近运行时间与非终态任务的互斥映射，
///         因此 admin 重启不会丢失调度现场。
///     </para>
///     <para>
///         选节点在插入 sys_task 之前完成：表结构要求 node_id NOT NULL，先建任务再选节点
///         无法落库。没有在线 crawler 节点时本轮跳过，不产生无主任务。
///     </para>
///     <para>
///         派发失败只把任务置为 push_failed，guard 仍持有；后续由对账循环按超时重派，
///         避免调度循环自己实现重试策略。
///     </para>
/// </remarks>
/// <param name="channels">渠道读取仓储；只读取 status=active 的渠道。</param>
/// <param name="tasks">任务仓储；用于恢复最近运行时间与非终态任务。</param>
/// <param name="adminTasks">admin 侧任务生命周期入口。</param>
/// <param name="nodeDirectory">在线节点目录；来自 gRPC 连接注册表。</param>
/// <param name="dispatcher">下行派发抽象。</param>
/// <param name="roundGuard">同渠道互斥；启动时从非终态任务恢复。</param>
/// <param name="runtimeCache">进程内最近运行时间缓存。</param>
/// <param name="evaluator">schedule 到期判断。</param>
/// <param name="planner">任务计划生成器。</param>
/// <param name="nodeSelector">节点选择策略。</param>
/// <param name="timeProvider">可测试时钟。</param>
/// <param name="options">调度循环配置。</param>
/// <param name="logger">结构化日志。</param>
public sealed class SchedulerLoop(
    IChannelRepository channels,
    ITaskRepository tasks,
    IAdminTaskService adminTasks,
    INodeDirectory nodeDirectory,
    INodeDispatcher dispatcher,
    ChannelRoundGuard roundGuard,
    ChannelRuntimeCache runtimeCache,
    ChannelScheduleEvaluator evaluator,
    TaskPlanner planner,
    NodeSelector nodeSelector,
    TimeProvider timeProvider,
    IOptions<SchedulerOptions> options,
    ILogger<SchedulerLoop> logger)
    : PeriodicBackgroundService(TimeSpan.FromSeconds(options.Value.TickSeconds), logger)
{
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _initialized;

    /// <summary>
    ///     执行一轮调度。
    /// </summary>
    /// <remarks>
    ///     单轮内按渠道依次处理；单个渠道的异常不会中断其他渠道。数据库或 gRPC 的瞬时异常
    ///     由 <see cref="PeriodicBackgroundService" /> 捕获并在下一轮重试。
    /// </remarks>
    /// <param name="cancellationToken">宿主停止令牌。</param>
    protected override async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken);

        var activeChannels = await channels.ListActiveForSchedulingAsync(cancellationToken);
        var availableNodes = nodeDirectory.GetOnlineNodes(NodeTypes.Crawler).ToList();
        var now = timeProvider.GetUtcNow();

        foreach (var channel in activeChannels)
        {
            if (roundGuard.IsHeld(channel.Id))
            {
                AdminLog.SchedulerSkipped(logger, channel.Id, "channel_round_in_progress");
                continue;
            }

            ChannelSchedule schedule;
            try
            {
                schedule = ChannelSchedule.Parse(channel.ScheduleJson);
            }
            catch (Exception exception) when (exception is JsonException or FormatException)
            {
                // 单渠道配置错误不能拖垮整个调度循环；记录后跳过，等运维修正配置。
                AdminLog.ScheduleInvalid(logger, channel.Id, exception.Message);
                continue;
            }

            if (!evaluator.IsDue(schedule, now, runtimeCache.GetLastRun(channel.Id)))
            {
                continue;
            }

            var node = nodeSelector.Select(availableNodes);
            if (node is null)
            {
                // 没有在线 crawler 节点时不插入任务，避免产生无法派发的无主任务。
                AdminLog.SchedulerSkipped(logger, channel.Id, "no_online_crawler_node");
                continue;
            }

            var plan = planner.CreateListRound(channel);
            var task = await adminTasks.CreateForDispatchAsync(plan, node.NodeId, cancellationToken);

            if (!roundGuard.TryAcquire(channel.Id, task.Id))
            {
                AdminLog.SchedulerSkipped(logger, channel.Id, "round_guard_conflict");
                await adminTasks.TryCancelAsync(task.Id, "round_guard_conflict", cancellationToken);
                continue;
            }

            runtimeCache.MarkStarted(channel.Id, now);
            availableNodes = ConsumeNodeCapacity(availableNodes, node.NodeId);

            var extra = AdminJson.Deserialize<TaskExtraView>(task.ExtraJson);
            if (extra?.DispatchToken is null)
            {
                AdminLog.TaskTokenMissing(logger, task.Id);
                continue;
            }

            var token = DispatchToken.From(extra.DispatchToken);
            var request = new DispatchRequest(task.Id, task.TaskType, task.TaskDetailJson, token);
            var sent = await dispatcher.DispatchAsync(node.NodeId, request, cancellationToken);
            if (sent)
            {
                // 节点可能在写流返回前就已经进入 running；此时 MarkPushed 返回 Conflict 是正常竞态。
                var mark = await adminTasks.MarkPushedAsync(task.Id, token, cancellationToken);
                if (!mark.Applied)
                {
                    AdminLog.SchedulerSkipped(logger, channel.Id, "mark_pushed_conflict");
                }

                continue;
            }

            AdminLog.DispatchFailed(logger, task.Id, node.NodeId, "gRPC 下行写入失败");
            await adminTasks.MarkPushFailedAsync(task.Id, token, "gRPC 下行写入失败", cancellationToken);
        }
    }

    /// <summary>
    ///     首次调度前恢复运行现场。
    /// </summary>
    /// <remarks>
    ///     需要恢复两样东西：每个渠道的最近运行时间（用于计算下次到期）和非终态任务占用的
    ///     渠道互斥（防止重启后立即重复派发）。初始化失败时不置位，下一轮会重试。
    /// </remarks>
    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
            {
                return;
            }

            var lastRuns = await tasks.GetLastRunTimesAsync(cancellationToken);
            runtimeCache.Reset(lastRuns);
            await roundGuard.RestoreFromTasksAsync(tasks, cancellationToken);
            _initialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    /// <summary>
    ///     在同一轮内模拟节点负载增量，尽量把多个渠道分散到不同节点。
    /// </summary>
    private static List<NodeSnapshot> ConsumeNodeCapacity(List<NodeSnapshot> nodes, Guid nodeId)
    {
        return nodes
            .Select(node => node.NodeId == nodeId
                ? node with { RunningTasks = node.RunningTasks + 1 }
                : node)
            .ToList();
    }
}
