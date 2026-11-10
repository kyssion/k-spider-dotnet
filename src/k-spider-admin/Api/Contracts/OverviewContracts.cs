namespace KSpider.Admin.Api.Contracts;

/// <summary>总览响应。</summary>
/// <param name="GeneratedAt">响应生成时间（UTC）。</param>
/// <param name="Channels">渠道状态计数。</param>
/// <param name="Tasks">任务状态计数。</param>
/// <param name="Nodes">节点状态计数。</param>
/// <param name="RecentFailures">最近失败/放弃任务，最多 10 条。</param>
public sealed record OverviewResponse(
    DateTime GeneratedAt,
    ChannelOverview Channels,
    TaskOverview Tasks,
    NodeOverview Nodes,
    IReadOnlyList<RecentFailureView> RecentFailures);

/// <summary>渠道状态计数。</summary>
/// <param name="Total">渠道总数。</param>
/// <param name="Active">启用数量。</param>
/// <param name="Paused">暂停数量。</param>
/// <param name="Retired">退役数量。</param>
public sealed record ChannelOverview(long Total, long Active, long Paused, long Retired);

/// <summary>任务状态计数。</summary>
/// <param name="Total">任务总数。</param>
/// <param name="ByStatus">八个任务状态的计数，键为数据库状态值。</param>
public sealed record TaskOverview(long Total, IReadOnlyDictionary<string, long> ByStatus);

/// <summary>节点状态计数。</summary>
/// <param name="Total">节点总数。</param>
/// <param name="Online">在线数量。</param>
/// <param name="Offline">离线数量。</param>
public sealed record NodeOverview(long Total, long Online, long Offline);

/// <summary>最近失败任务视图。</summary>
/// <param name="TaskId">任务主键。</param>
/// <param name="ChannelId">渠道主键。</param>
/// <param name="NodeId">执行节点 uuid。</param>
/// <param name="Status">任务状态。</param>
/// <param name="FailCount">失败次数。</param>
/// <param name="LastError">最近错误。</param>
/// <param name="UpdatedAt">更新时间（UTC）。</param>
public sealed record RecentFailureView(
    long TaskId,
    long ChannelId,
    Guid NodeId,
    string Status,
    int FailCount,
    string? LastError,
    DateTime UpdatedAt);
