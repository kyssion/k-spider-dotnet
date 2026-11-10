using System.ComponentModel.DataAnnotations;

namespace KSpider.Admin.Scheduler;

/// <summary>
///     调度循环配置。
/// </summary>
public sealed class SchedulerOptions
{
    /// <summary>配置节名。</summary>
    public const string SectionName = "Admin:Scheduler";

    /// <summary>
    ///     调度循环周期（秒）。周期越短，到期判断越及时，但数据库查询更频繁。
    /// </summary>
    [Range(1, 3600)]
    public int TickSeconds { get; set; } = 5;

    /// <summary>
    ///     默认任务包批量上限；渠道级覆盖在 channel.extra 键表定稿后接入。
    /// </summary>
    [Range(1, 100_000)]
    public int DefaultBatchLimit { get; set; } = 1000;
}
