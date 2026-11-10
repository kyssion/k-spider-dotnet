using KSpider.Admin.Api.Contracts;
using KSpider.Admin.Repository.Abstractions;
using KSpider.Admin.Tasks;

namespace KSpider.Admin.Api.Queries;

/// <summary>
///     总览响应映射。
/// </summary>
/// <param name="overview">总览仓储。</param>
/// <param name="timeProvider">可测试时钟。</param>
public sealed class OverviewQueryService(
    IOverviewRepository overview,
    TimeProvider timeProvider)
{
    private static readonly string[] StatusKeys = Enum
        .GetValues<AdminTaskStatus>()
        .Select(status => status.ToDbValue())
        .ToArray();

    /// <summary>
    ///     读取一次总览快照并补齐所有任务状态计数（缺失状态返回 0）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<OverviewResponse> GetAsync(CancellationToken cancellationToken)
    {
        var snapshot = await overview.GetAsync(cancellationToken);
        var byStatus = StatusKeys.ToDictionary(
            key => key,
            key => snapshot.TasksByStatus.GetValueOrDefault(key),
            StringComparer.Ordinal);

        return new OverviewResponse(
            timeProvider.GetUtcNow().UtcDateTime,
            new ChannelOverview(
                snapshot.Channels.Total,
                snapshot.Channels.Active,
                snapshot.Channels.Paused,
                snapshot.Channels.Retired),
            new TaskOverview(snapshot.TasksTotal, byStatus),
            new NodeOverview(snapshot.Nodes.Total, snapshot.Nodes.Online, snapshot.Nodes.Offline),
            snapshot.RecentFailures
                .Select(failure => new RecentFailureView(
                    failure.TaskId,
                    failure.ChannelId,
                    failure.NodeId,
                    failure.Status,
                    failure.FailCount,
                    failure.LastError,
                    failure.UpdatedAt))
                .ToList());
    }
}
