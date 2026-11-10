namespace KSpider.Admin.Scheduler;

/// <summary>
///     判断渠道本轮是否到期。
/// </summary>
/// <remarks>
///     当前语义是固定 interval：首次运行立即执行，之后必须等满 interval_seconds。
///     cron、时区与错过轮次补跑仍是待确认项。
/// </remarks>
public sealed class ChannelScheduleEvaluator
{
    /// <summary>
    ///     判断是否到期。
    /// </summary>
    /// <param name="schedule">渠道调度配置。</param>
    /// <param name="now">当前时间。</param>
    /// <param name="lastRunAt">最近一次任务创建时间；无历史时为 null。</param>
    /// <returns>到期返回 true。</returns>
    public bool IsDue(ChannelSchedule schedule, DateTimeOffset now, DateTimeOffset? lastRunAt)
    {
        if (!schedule.Enabled)
        {
            return false;
        }

        return lastRunAt is null || now >= lastRunAt.Value.AddSeconds(schedule.IntervalSeconds);
    }
}
