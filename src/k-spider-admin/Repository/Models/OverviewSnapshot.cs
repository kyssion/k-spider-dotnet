namespace KSpider.Admin.Repository.Models;

/// <summary>
///     总览统计快照；计数是同一请求内的近似值，不要求跨表事务一致。
/// </summary>
/// <param name="Channels">渠道状态计数。</param>
/// <param name="TasksTotal">任务总数。</param>
/// <param name="TasksByStatus">按状态计数的字典，键为数据库状态值。</param>
/// <param name="Nodes">节点状态计数。</param>
/// <param name="RecentFailures">最近失败/放弃任务，最多 10 条。</param>
public sealed record OverviewSnapshot(
    ChannelStatusCounts Channels,
    long TasksTotal,
    IReadOnlyDictionary<string, long> TasksByStatus,
    NodeStatusCounts Nodes,
    IReadOnlyList<RecentTaskFailure> RecentFailures);

/// <summary>渠道状态计数。</summary>
/// <param name="Total">渠道总数。</param>
/// <param name="Active">active 数量。</param>
/// <param name="Paused">paused 数量。</param>
/// <param name="Retired">retired 数量。</param>
public sealed record ChannelStatusCounts(long Total, long Active, long Paused, long Retired);

/// <summary>节点状态计数。</summary>
/// <param name="Total">节点总数。</param>
/// <param name="Online">online 数量。</param>
/// <param name="Offline">offline 数量。</param>
public sealed record NodeStatusCounts(long Total, long Online, long Offline);

/// <summary>最近失败任务摘要。</summary>
/// <param name="TaskId">任务主键。</param>
/// <param name="ChannelId">渠道主键；解析失败时为 0。</param>
/// <param name="NodeId">执行节点 uuid。</param>
/// <param name="Status">任务状态。</param>
/// <param name="FailCount">失败次数。</param>
/// <param name="LastError">最近错误，超长截断。</param>
/// <param name="UpdatedAt">任务更新时间（UTC）。</param>
public sealed record RecentTaskFailure(
    long TaskId,
    long ChannelId,
    Guid NodeId,
    string Status,
    int FailCount,
    string? LastError,
    DateTime UpdatedAt);
