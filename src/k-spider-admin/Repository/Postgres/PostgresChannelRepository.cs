using System.Data.Common;
using System.Text;
using KSpider.Admin.Repository.Abstractions;
using KSpider.Admin.Repository.Models;
using LinqToDB;
using LinqToDB.Data;

namespace KSpider.Admin.Repository.Postgres;

/// <summary>
///     channel / channel_category 数据访问；调度只读取 active 渠道。
/// </summary>
internal sealed class PostgresChannelRepository(IAdminDbConnectionFactory connectionFactory)
    : PostgresRepositoryBase(connectionFactory), IChannelRepository
{
    private const string ChannelColumns =
        "id, category_id, channel_name, source_adapter, detail_adapter, write_mode, retention_days, schedule::text AS schedule, status, extra::text AS extra, create_time, update_time";


    public Task<IReadOnlyList<ChannelRecord>> ListActiveForSchedulingAsync(CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT {ChannelColumns}
            FROM channel
            WHERE status='active'
            ORDER BY id
            """;

        return ExecuteAsync(
            async db => (IReadOnlyList<ChannelRecord>)await db.QueryToListAsync(
                ReadChannel,
                sql,
                cancellationToken),
            cancellationToken);
    }

    public Task<ChannelRecord?> GetAsync(long id, CancellationToken cancellationToken)
    {
        var sql = $"SELECT {ChannelColumns} FROM channel WHERE id=@id::bigint";
        return ExecuteAsync(async db =>
        {
            var rows = await db.QueryToListAsync(
                ReadChannel,
                sql,
                cancellationToken,
                Parameter("id", id));
            return rows.Count > 0 ? rows[0] : null;
        }, cancellationToken);
    }

    public Task<PageResult<ChannelRecord>> QueryAsync(ChannelQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var where = new StringBuilder("WHERE 1=1");
        var parameters = new List<DataParameter>();

        if (query.CategoryId is { } categoryId)
        {
            where.Append(" AND category_id=@categoryId::bigint");
            parameters.Add(Parameter("categoryId", categoryId));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            where.Append(" AND status=@status::text");
            parameters.Add(Parameter("status", query.Status));
        }

        var countSql = $"SELECT count(*)::bigint FROM channel {where}";
        var listSql = $"""
            SELECT {ChannelColumns}
            FROM channel
            {where}
            ORDER BY id
            LIMIT @limit::int OFFSET @offset::int
            """;

        return ExecuteAsync(async db =>
        {
            var total = await db.ExecuteAsync<long>(countSql, cancellationToken, parameters.ToArray());
            var listParameters = new List<DataParameter>(parameters)
            {
                Parameter("limit", pageSize),
                Parameter("offset", (page - 1) * pageSize),
            };
            var items = await db.QueryToListAsync(
                ReadChannel,
                listSql,
                cancellationToken,
                listParameters.ToArray());
            return new PageResult<ChannelRecord>(items, page, pageSize, total);
        }, cancellationToken);
    }

    public Task<IReadOnlyList<ChannelCategoryRecord>> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, parent_id, name, path, sort_no, status
            FROM channel_category
            ORDER BY path
            """;

        return ExecuteAsync(
            async db => (IReadOnlyList<ChannelCategoryRecord>)await db.QueryToListAsync(
                reader => new ChannelCategoryRecord(
                    reader.GetInt64(0),
                    reader.IsDBNull(1) ? null : reader.GetInt64(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetInt32(4),
                    reader.GetString(5)),
                sql,
                cancellationToken),
            cancellationToken);
    }

    public Task<bool> UpdateStatusAsync(long id, string status, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE channel
            SET status=@status::text, update_time=now()
            WHERE id=@id::bigint
            """;

        return ExecuteAsync(
            async db => await db.ExecuteAsync(
                sql,
                cancellationToken,
                Parameter("id", id),
                Parameter("status", status)) > 0,
            cancellationToken);
    }

    private static ChannelRecord ReadChannel(DbDataReader reader)
    {
        return new ChannelRecord(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            GetNullableInt32(reader, 6),
            reader.GetString(7),
            reader.GetString(8),
            reader.GetString(9),
            GetDateTime(reader, 10),
            GetDateTime(reader, 11));
    }
}
