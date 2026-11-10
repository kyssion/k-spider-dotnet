using KSpider.Admin.Common.Background;
using KSpider.Admin.Common.Logging;
using KSpider.Admin.Repository.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KSpider.Admin.Reconciliation;

/// <summary>
///     终态任务与 offline 节点清理；非终态行永不清。
/// </summary>
/// <remarks>
///     分批删除避免一次 DELETE 长时间持有大量行锁。任务与节点分别按保留天数、保留分钟数
///     计算截止时间；清理失败只影响本轮，下一轮会继续。
/// </remarks>
/// <param name="tasks">任务仓储。</param>
/// <param name="nodes">节点镜像仓储。</param>
/// <param name="options">清理周期与保留期配置。</param>
/// <param name="timeProvider">可测试时钟。</param>
/// <param name="logger">结构化日志。</param>
public sealed class TaskCleanupLoop(
    ITaskRepository tasks,
    ISysNodeRepository nodes,
    IOptions<ReconciliationOptions> options,
    TimeProvider timeProvider,
    ILogger<TaskCleanupLoop> logger)
    : PeriodicBackgroundService(TimeSpan.FromSeconds(options.Value.CleanupTickSeconds), logger)
{
    private readonly ReconciliationOptions _options = options.Value;

    /// <summary>
    ///     执行一轮清理。
    /// </summary>
    /// <param name="cancellationToken">宿主停止令牌。</param>
    protected override async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var taskCutoff = now.AddDays(-_options.TerminalRetentionDays);
        var nodeCutoff = now.AddMinutes(-_options.OfflineNodeRetentionMinutes);
        var deletedTasks = 0;

        while (true)
        {
            // 只删终态；循环直到不足一批，避免一次清理拖住数据库。
            var deleted = await tasks.DeleteTerminalOlderThanAsync(
                taskCutoff,
                _options.CleanupBatchSize,
                cancellationToken);
            deletedTasks += deleted;
            if (deleted < _options.CleanupBatchSize)
            {
                break;
            }
        }

        var deletedNodes = await nodes.DeleteOfflineOlderThanAsync(nodeCutoff, cancellationToken);
        AdminLog.CleanupCompleted(logger, deletedTasks, deletedNodes);
    }
}
