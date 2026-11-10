using System.ComponentModel.DataAnnotations;

namespace KSpider.Admin.Reconciliation;

/// <summary>
///     对账、探测、重派与清理配置。
/// </summary>
/// <remarks>
///     这些默认值是 admin 循环级参数，不写入 sys_task.extra；运行上限快照在派发时进入
///     task_detail。具体默认值仍属于待确认项，部署方可通过配置覆盖。
/// </remarks>
public sealed class ReconciliationOptions
{
    /// <summary>配置节名。</summary>
    public const string SectionName = "Admin:Reconciliation";

    /// <summary>对账扫描周期（秒）。</summary>
    [Range(1, 3600)]
    public int TickSeconds { get; set; } = 10;

    /// <summary>任务超过该秒数没有 update_time 更新即视为无活动。</summary>
    [Range(1, 86_400)]
    public int InactivityTimeoutSeconds { get; set; } = 300;

    /// <summary>单次 probe_request 的超时（秒）。</summary>
    [Range(1, 60)]
    public int ProbeTimeoutSeconds { get; set; } = 5;

    /// <summary>一次对账最多探测次数。</summary>
    [Range(1, 10)]
    public int ProbeAttempts { get; set; } = 3;

    /// <summary>探测重试总窗口（秒）；用于计算每次重试间隔。</summary>
    [Range(1, 300)]
    public int ProbeWindowSeconds { get; set; } = 30;

    /// <summary>超过该失败次数后转 abandoned。</summary>
    [Range(1, 100)]
    public int MaxFailCount { get; set; } = 3;

    /// <summary>单轮扫描的最大任务数。</summary>
    [Range(1, 1000)]
    public int ScanBatchSize { get; set; } = 100;

    /// <summary>节点心跳超时（秒）；超时后判死并重派名下任务。</summary>
    [Range(1, 3600)]
    public int HeartbeatTimeoutSeconds { get; set; } = 30;

    /// <summary>offline 墓碑保留时间（分钟）。</summary>
    [Range(1, 1440)]
    public int OfflineNodeRetentionMinutes { get; set; } = 10;

    /// <summary>清理循环周期（秒）。</summary>
    [Range(1, 3600)]
    public int CleanupTickSeconds { get; set; } = 3600;

    /// <summary>终态任务保留天数。</summary>
    [Range(1, 365)]
    public int TerminalRetentionDays { get; set; } = 7;

    /// <summary>清理时单批删除行数。</summary>
    [Range(1, 10_000)]
    public int CleanupBatchSize { get; set; } = 1000;
}
