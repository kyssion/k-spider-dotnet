using System.Collections.Concurrent;

namespace KSpider.Admin.Scheduler;

/// <summary>
///     进程内最近运行时间缓存；启动时从 sys_task 恢复，建任务后立即更新。
/// </summary>
/// <remarks>
///     缓存不是事实源：admin 重启后会用 sys_task 的 create_time 重建；它只避免每个 tick
///     对每个渠道重复查询 JSONB。数据库时间按 UTC 处理。
/// </remarks>
public sealed class ChannelRuntimeCache
{
    private readonly ConcurrentDictionary<long, DateTimeOffset> _lastRuns = new();

    /// <summary>
    ///     用启动时读取的最近运行时间重置缓存。
    /// </summary>
    /// <param name="lastRuns">channel_id → 最近任务创建时间。</param>
    public void Reset(IReadOnlyDictionary<long, DateTime> lastRuns)
    {
        _lastRuns.Clear();
        foreach (var (channelId, lastRun) in lastRuns)
        {
            _lastRuns[channelId] = ToOffset(lastRun);
        }
    }

    /// <summary>
    ///     读取渠道最近运行时间。
    /// </summary>
    /// <param name="channelId">渠道主键。</param>
    /// <returns>最近运行时间；没有历史时为 null。</returns>
    public DateTimeOffset? GetLastRun(long channelId)
    {
        return _lastRuns.TryGetValue(channelId, out var lastRun) ? lastRun : null;
    }

    /// <summary>
    ///     记录一轮已启动。
    /// </summary>
    /// <param name="channelId">渠道主键。</param>
    /// <param name="now">启动时间。</param>
    public void MarkStarted(long channelId, DateTimeOffset now)
    {
        _lastRuns[channelId] = now;
    }

    private static DateTimeOffset ToOffset(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? new DateTimeOffset(value)
            : new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
