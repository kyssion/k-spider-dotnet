using System.Data.Common;
using System.Text;
using KSpider.Admin.Repository.Abstractions;
using KSpider.Admin.Repository.Models;
using LinqToDB;
using LinqToDB.Data;

namespace KSpider.Admin.Repository.Postgres;

/// <summary>
///     sys_task 数据访问；状态迁移使用条件 UPDATE，extra 使用 JSONB 增量合并。
/// </summary>
internal sealed class PostgresTaskRepository(IAdminDbConnectionFactory connectionFactory)
    : PostgresRepositoryBase(connectionFactory), ITaskRepository
{
    private const string TaskColumns =
        "id, node_id, task_type, task_detail::text AS task_detail, status, extra::text AS extra, create_time, update_time";

    private const string NonTerminalStatuses = "'pushing','push_failed','pushed','running','run_failed'";
    private const string TerminalStatuses = "'success','abandoned','cancelled'";


    public Task<SysTaskRecord> InsertAsync(SysTaskInsert insert, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO sys_task (node_id, task_type, task_detail, status, extra)
            VALUES (@nodeId::uuid, @taskType::text, @taskDetail::jsonb, @status::text, @extra::jsonb)
            RETURNING id, node_id, task_type, task_detail::text AS task_detail, status, extra::text AS extra, create_time, update_time
            """;

        return ExecuteAsync(async db =>
        {
            var rows = await db.QueryToListAsync(
                ReadTask,
                sql,
                cancellationToken,
                Parameter("nodeId", insert.NodeId),
                Parameter("taskType", insert.TaskType),
                Parameter("taskDetail", insert.TaskDetailJson),
                Parameter("status", insert.Status),
                Parameter("extra", insert.ExtraJson));

            return rows.Count > 0
                ? rows[0]
                : throw new InvalidOperationException("INSERT sys_task 未返回行");
        }, cancellationToken);
    }

    public Task<SysTaskRecord?> GetAsync(long id, CancellationToken cancellationToken)
    {
        var sql = $"SELECT {TaskColumns} FROM sys_task WHERE id=@id::bigint";
        return ExecuteAsync(async db =>
        {
            var rows = await db.QueryToListAsync(
                ReadTask,
                sql,
                cancellationToken,
                Parameter("id", id));
            return rows.Count > 0 ? rows[0] : null;
        }, cancellationToken);
    }

    public Task<IReadOnlyList<SysTaskRecord>> FindNonTerminalAsync(CancellationToken cancellationToken)
    {
        var sql = $"SELECT {TaskColumns} FROM sys_task WHERE status IN ({NonTerminalStatuses}) ORDER BY id";
        return ExecuteAsync(
            async db => (IReadOnlyList<SysTaskRecord>)await db.QueryToListAsync(
                ReadTask,
                sql,
                cancellationToken),
            cancellationToken);
    }

    public Task<IReadOnlyList<SysTaskRecord>> FindNonTerminalByNodeAsync(
        Guid nodeId,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT {TaskColumns}
            FROM sys_task
            WHERE status IN ({NonTerminalStatuses})
              AND node_id=@nodeId::uuid
            ORDER BY id
            """;

        return ExecuteAsync(
            async db => (IReadOnlyList<SysTaskRecord>)await db.QueryToListAsync(
                ReadTask,
                sql,
                cancellationToken,
                Parameter("nodeId", nodeId)),
            cancellationToken);
    }

    public Task<IReadOnlyList<SysTaskRecord>> FindStaleAsync(
        DateTime beforeUtc,
        int limit,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT {TaskColumns}
            FROM sys_task
            WHERE status IN ({NonTerminalStatuses})
              AND update_time < @before::timestamptz
            ORDER BY update_time, id
            LIMIT @limit::int
            """;

        return ExecuteAsync(
            async db => (IReadOnlyList<SysTaskRecord>)await db.QueryToListAsync(
                ReadTask,
                sql,
                cancellationToken,
                Parameter("before", beforeUtc),
                Parameter("limit", limit)),
            cancellationToken);
    }

    public Task<IReadOnlyDictionary<long, DateTime>> GetLastRunTimesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT (task_detail->>'channel_id')::bigint AS channel_id, max(create_time) AS last_run
            FROM sys_task
            WHERE jsonb_exists(task_detail, 'channel_id')
            GROUP BY 1
            """;

        return ExecuteAsync(async db =>
        {
            var rows = await db.QueryToListAsync(
                reader => new ChannelLastRun(reader.GetInt64(0), GetDateTime(reader, 1)),
                sql,
                cancellationToken);
            return (IReadOnlyDictionary<long, DateTime>)rows.ToDictionary(row => row.ChannelId, row => row.LastRun);
        }, cancellationToken);
    }

    public Task<bool> TryMarkPushedAsync(long id, string token, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE sys_task
            SET status='pushed',
                update_time=now(),
                extra = extra || jsonb_build_object('pushed_at', to_jsonb(now()))
            WHERE id=@id::bigint
              AND status='pushing'
              AND extra->>'dispatch_token' = @token::text
            """;

        return ExecuteAffectedAsync(sql, cancellationToken, Parameter("id", id), Parameter("token", token));
    }

    public Task<bool> TryMarkPushFailedAsync(
        long id,
        string token,
        string failure,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE sys_task
            SET status='push_failed',
                update_time=now(),
                extra = extra || jsonb_build_object(
                    'push_failed_at', to_jsonb(now()),
                    'last_error', @error::text)
            WHERE id=@id::bigint
              AND status='pushing'
              AND extra->>'dispatch_token' = @token::text
            """;

        return ExecuteAffectedAsync(
            sql,
            cancellationToken,
            Parameter("id", id),
            Parameter("token", token),
            Parameter("error", failure));
    }

    public Task<bool> TryCancelAsync(long id, string reason, CancellationToken cancellationToken)
    {
        var sql = $"""
            UPDATE sys_task
            SET status='cancelled',
                update_time=now(),
                extra = extra || jsonb_build_object(
                    'cancelled_at', to_jsonb(now()),
                    'last_error', @reason::text)
            WHERE id=@id::bigint
              AND status NOT IN ({TerminalStatuses})
            """;

        return ExecuteAffectedAsync(
            sql,
            cancellationToken,
            Parameter("id", id),
            Parameter("reason", reason));
    }

    public Task<bool> TryRedispatchAsync(
        long id,
        Guid nodeId,
        string taskDetailJson,
        string token,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE sys_task
            SET node_id=@nodeId::uuid,
                status='pushing',
                task_detail=@taskDetail::jsonb,
                update_time=now(),
                extra = (extra - 'dispatch_token' - 'last_error') ||
                        jsonb_build_object(
                            'dispatch_token', @token::text,
                            'fail_count', COALESCE((extra->>'fail_count')::int, 0) + 1,
                            'redispatched_at', to_jsonb(now()))
            WHERE id=@id::bigint
              AND status IN ('pushing','push_failed','run_failed','pushed')
            """;

        return ExecuteAffectedAsync(
            sql,
            cancellationToken,
            Parameter("id", id),
            Parameter("nodeId", nodeId),
            Parameter("taskDetail", taskDetailJson),
            Parameter("token", token));
    }

    public Task<bool> TryAbandonAsync(long id, string reason, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE sys_task
            SET status='abandoned',
                update_time=now(),
                extra = extra || jsonb_build_object(
                    'abandoned_at', to_jsonb(now()),
                    'last_error', @reason::text)
            WHERE id=@id::bigint
              AND status IN ('pushing','push_failed','run_failed','pushed')
            """;

        return ExecuteAffectedAsync(
            sql,
            cancellationToken,
            Parameter("id", id),
            Parameter("reason", reason));
    }

    public Task<int> DeleteTerminalOlderThanAsync(
        DateTime cutoffUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            DELETE FROM sys_task
            WHERE id IN (
                SELECT id
                FROM sys_task
                WHERE status IN ({TerminalStatuses})
                  AND update_time < @cutoff::timestamptz
                ORDER BY update_time, id
                LIMIT @limit::int
            )
            """;

        return ExecuteAsync(
            db => db.ExecuteAsync(
                sql,
                cancellationToken,
                Parameter("cutoff", cutoffUtc),
                Parameter("limit", batchSize)),
            cancellationToken);
    }

    public Task<PageResult<SysTaskRecord>> QueryAsync(TaskQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var where = new StringBuilder("WHERE 1=1");
        var parameters = new List<DataParameter>();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            where.Append(" AND status=@status::text");
            parameters.Add(Parameter("status", query.Status));
        }

        if (query.NodeId is { } nodeId)
        {
            where.Append(" AND node_id=@nodeId::uuid");
            parameters.Add(Parameter("nodeId", nodeId));
        }

        var countSql = $"SELECT count(*)::bigint FROM sys_task {where}";
        var listSql = $"""
            SELECT {TaskColumns}
            FROM sys_task
            {where}
            ORDER BY id DESC
            LIMIT @limit::int OFFSET @offset::int
            """;

        return ExecuteAsync(async db =>
        {
            var total = await db.ExecuteAsync<long>(
                countSql,
                cancellationToken,
                parameters.ToArray());

            var listParameters = new List<DataParameter>(parameters)
            {
                Parameter("limit", pageSize),
                Parameter("offset", (page - 1) * pageSize),
            };

            var items = await db.QueryToListAsync(
                ReadTask,
                listSql,
                cancellationToken,
                listParameters.ToArray());

            return new PageResult<SysTaskRecord>(items, page, pageSize, total);
        }, cancellationToken);
    }

    private Task<bool> ExecuteAffectedAsync(
        string sql,
        CancellationToken cancellationToken,
        params DataParameter[] parameters)
    {
        return ExecuteAsync(
            async db => await db.ExecuteAsync(sql, cancellationToken, parameters) > 0,
            cancellationToken);
    }

    private static SysTaskRecord ReadTask(DbDataReader reader)
    {
        return new SysTaskRecord(
            reader.GetInt64(0),
            reader.GetGuid(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            GetDateTime(reader, 6),
            GetDateTime(reader, 7));
    }

    private sealed record ChannelLastRun(long ChannelId, DateTime LastRun);
}
