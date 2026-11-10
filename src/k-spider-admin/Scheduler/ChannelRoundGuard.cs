using System.Collections.Concurrent;
using KSpider.Admin.Common.Json;
using KSpider.Admin.Common.Logging;
using KSpider.Admin.Repository.Abstractions;
using KSpider.Admin.Tasks;
using Microsoft.Extensions.Logging;

namespace KSpider.Admin.Scheduler;

/// <summary>
///     同渠道互斥：admin 单实例，内存即可；启动时从非终态任务恢复。
/// </summary>
/// <remarks>
///     互斥生命周期从任务创建开始，一直持有到 success / abandoned / cancelled 终态。
///     run_failed / push_failed 仍持有，因为对账会继续重派同一个任务，不能提前释放。
/// </remarks>
/// <param name="logger">结构化日志。</param>
public sealed class ChannelRoundGuard(ILogger<ChannelRoundGuard> logger) : ITaskTerminalObserver
{
    private readonly ConcurrentDictionary<long, long> _activeTasks = new();

    /// <summary>
    ///     判断渠道当前是否有非终态任务。
    /// </summary>
    /// <param name="channelId">渠道主键。</param>
    public bool IsHeld(long channelId)
    {
        return _activeTasks.ContainsKey(channelId);
    }

    /// <summary>
    ///     尝试占用渠道互斥。
    /// </summary>
    /// <param name="channelId">渠道主键。</param>
    /// <param name="taskId">任务主键。</param>
    /// <returns>占用成功或已由同一任务持有时返回 true。</returns>
    public bool TryAcquire(long channelId, long taskId)
    {
        if (_activeTasks.TryAdd(channelId, taskId))
        {
            return true;
        }

        return _activeTasks.TryGetValue(channelId, out var existing) && existing == taskId;
    }

    /// <summary>
    ///     读取渠道当前持有的任务。
    /// </summary>
    /// <param name="channelId">渠道主键。</param>
    /// <param name="taskId">任务主键。</param>
    /// <returns>存在持有时返回 true。</returns>
    public bool TryGetActiveTaskId(long channelId, out long taskId)
    {
        return _activeTasks.TryGetValue(channelId, out taskId);
    }

    /// <summary>
    ///     释放渠道互斥；只有持有者与传入 taskId 一致时才移除。
    /// </summary>
    /// <param name="channelId">渠道主键。</param>
    /// <param name="taskId">任务主键。</param>
    public void Release(long channelId, long taskId)
    {
        ((ICollection<KeyValuePair<long, long>>)_activeTasks).Remove(
            new KeyValuePair<long, long>(channelId, taskId));
    }

    /// <summary>
    ///     启动时从非终态任务恢复互斥映射。
    /// </summary>
    /// <remarks>
    ///     同一渠道出现多条非终态任务属于数据异常；保留先恢复的一条并记录冲突，
    ///     避免重启后重复派发。无法解析 task_detail 的任务会被跳过。
    /// </remarks>
    /// <param name="taskRepository">任务仓储。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task RestoreFromTasksAsync(
        ITaskRepository taskRepository,
        CancellationToken cancellationToken)
    {
        var tasks = await taskRepository.FindNonTerminalAsync(cancellationToken);
        foreach (var task in tasks)
        {
            TaskDetail? detail;
            try
            {
                detail = AdminJson.Deserialize<TaskDetail>(task.TaskDetailJson);
            }
            catch (System.Text.Json.JsonException)
            {
                continue;
            }

            if (detail is null)
            {
                continue;
            }

            if (TryAcquire(detail.ChannelId, task.Id))
            {
                continue;
            }

            if (TryGetActiveTaskId(detail.ChannelId, out var existingTaskId))
            {
                AdminLog.GuardRestoreConflict(logger, detail.ChannelId, existingTaskId, task.Id);
            }
        }
    }

    /// <summary>
    ///     任务终态时释放互斥。
    /// </summary>
    /// <param name="taskId">任务主键。</param>
    /// <param name="channelId">渠道主键。</param>
    /// <param name="cancellationToken">取消令牌；本实现不使用。</param>
    public ValueTask OnTaskTerminalAsync(long taskId, long channelId, CancellationToken cancellationToken)
    {
        Release(channelId, taskId);
        return ValueTask.CompletedTask;
    }
}
