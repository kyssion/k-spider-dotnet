using System.Data.Common;
using KSpider.Admin.Repository.Abstractions;
using KSpider.Admin.Repository.Models;
using LinqToDB;
using LinqToDB.Data;

namespace KSpider.Admin.Repository.Postgres;

/// <summary>
///     总览统计查询；多个聚合在同一连接内执行，结果是近似快照。
/// </summary>
internal sealed class PostgresOverviewRepository(IAdminDbConnectionFactory connectionFactory)
    : PostgresRepositoryBase(connectionFactory), IOverviewRepository
{
    private const string RecentFailuresSql = """
        SELECT id,
               COALESCE((task_detail->>'channel_id')::bigint, 0) AS channel_id,
               node_id,
               status,
               COALESCE((extra->>'fail_count')::int, 0) AS fail_count,
               left(extra->>'last_error', 500) AS last_error,
               update_time
        FROM sys_task
        WHERE status IN ('push_failed','run_failed','abandoned')
        ORDER BY update_time DESC, id DESC
        LIMIT 10
        """;

    public Task<OverviewSnapshot> GetAsync(CancellationToken cancellationToken)
    {
        return ExecuteAsync(async db =>
        {
            var channelCounts = await QueryCountsAsync(db, "channel", cancellationToken);
            var taskCounts = await QueryCountsAsync(db, "sys_task", cancellationToken);
            var nodeCounts = await QueryCountsAsync(db, "sys_node", cancellationToken);
            var recentFailures = await db.QueryToListAsync(
                ReadRecentFailure,
                RecentFailuresSql,
                cancellationToken);

            return BuildSnapshot(channelCounts, taskCounts, nodeCounts, recentFailures);
        }, cancellationToken);
    }

    private static Task<List<StatusCount>> QueryCountsAsync(
        DataConnection db,
        string table,
        CancellationToken cancellationToken)
    {
        var sql = $"SELECT status, count(*)::bigint FROM {table} GROUP BY status";
        return db.QueryToListAsync(
            reader => new StatusCount(reader.GetString(0), reader.GetInt64(1)),
            sql,
            cancellationToken);
    }

    private static OverviewSnapshot BuildSnapshot(
        IReadOnlyList<StatusCount> channelCounts,
        IReadOnlyList<StatusCount> taskCounts,
        IReadOnlyList<StatusCount> nodeCounts,
        IReadOnlyList<RecentTaskFailure> recentFailures)
    {
        var channels = new ChannelStatusCounts(
            channelCounts.Sum(item => item.Count),
            Count(channelCounts, "active"),
            Count(channelCounts, "paused"),
            Count(channelCounts, "retired"));

        var nodes = new NodeStatusCounts(
            nodeCounts.Sum(item => item.Count),
            Count(nodeCounts, "online"),
            Count(nodeCounts, "offline"));

        var byStatus = taskCounts.ToDictionary(
            item => item.Status,
            item => item.Count,
            StringComparer.Ordinal);

        return new OverviewSnapshot(
            channels,
            taskCounts.Sum(item => item.Count),
            byStatus,
            nodes,
            recentFailures);
    }

    private static long Count(IReadOnlyList<StatusCount> counts, string status)
    {
        return counts
            .Where(item => string.Equals(item.Status, status, StringComparison.Ordinal))
            .Sum(item => item.Count);
    }

    private static RecentTaskFailure ReadRecentFailure(DbDataReader reader)
    {
        return new RecentTaskFailure(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetGuid(2),
            reader.GetString(3),
            reader.GetInt32(4),
            GetNullableString(reader, 5),
            GetDateTime(reader, 6));
    }

    private sealed record StatusCount(string Status, long Count);
}
