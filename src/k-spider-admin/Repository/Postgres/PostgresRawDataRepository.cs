using System.Data.Common;
using System.Text;
using KSpider.Admin.Repository.Abstractions;
using KSpider.Admin.Repository.Models;
using LinqToDB;
using LinqToDB.Data;

namespace KSpider.Admin.Repository.Postgres;

/// <summary>
///     data_source / data_detail 只读查询；列表只取 raw 长度，详情才读取 raw。
/// </summary>
internal sealed class PostgresRawDataRepository(IAdminDbConnectionFactory connectionFactory)
    : PostgresRepositoryBase(connectionFactory), IRawDataRepository
{
    private const string SourceColumns =
        "ds.id, ds.channel_id, c.channel_name, ds.adapter, ds.record_key, ds.data_status, ds.exec_status, " +
        "octet_length(ds.raw)::int AS raw_size, ds.create_time, ds.update_time";

    private const string DetailColumns =
        "dd.id, dd.source_id, dd.fetch_url, dd.adapter, dd.record_key, dd.data_status, dd.exec_status, " +
        "octet_length(dd.raw)::int AS raw_size, dd.create_time, dd.update_time";

    public Task<PageResult<RawDataSourceRecord>> QuerySourcesAsync(
        RawDataSourceQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var where = new StringBuilder("WHERE 1=1");
        var parameters = new List<DataParameter>();

        if (query.ChannelId is { } channelId)
        {
            where.Append(" AND ds.channel_id=@channelId::bigint");
            parameters.Add(Parameter("channelId", channelId));
        }

        if (query.FromUtc is { } from)
        {
            where.Append(" AND ds.create_time >= @from::timestamptz");
            parameters.Add(Parameter("from", from));
        }

        if (query.ToUtc is { } to)
        {
            where.Append(" AND ds.create_time < @to::timestamptz");
            parameters.Add(Parameter("to", to));
        }

        if (!string.IsNullOrWhiteSpace(query.ExecStatus))
        {
            where.Append(" AND ds.exec_status=@execStatus::text");
            parameters.Add(Parameter("execStatus", query.ExecStatus));
        }

        if (!string.IsNullOrWhiteSpace(query.DataStatus))
        {
            where.Append(" AND ds.data_status=@dataStatus::text");
            parameters.Add(Parameter("dataStatus", query.DataStatus));
        }

        var countSql = $"SELECT count(*)::bigint FROM data_source ds {where}";
        var listSql = $"""
            SELECT {SourceColumns}
            FROM data_source ds
            JOIN channel c ON c.id = ds.channel_id
            {where}
            ORDER BY ds.create_time DESC, ds.id DESC
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
                ReadSourceList,
                listSql,
                cancellationToken,
                listParameters.ToArray());

            return new PageResult<RawDataSourceRecord>(items, page, pageSize, total);
        }, cancellationToken);
    }

    public Task<RawDataSourceRecord?> GetSourceAsync(long id, CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT {SourceColumns}, ds.raw
            FROM data_source ds
            JOIN channel c ON c.id = ds.channel_id
            WHERE ds.id=@id::bigint
            """;

        return ExecuteAsync(async db =>
        {
            var rows = await db.QueryToListAsync(
                ReadSourceDetail,
                sql,
                cancellationToken,
                Parameter("id", id));
            return rows.Count > 0 ? rows[0] : null;
        }, cancellationToken);
    }

    public Task<bool> SourceExistsAsync(long id, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM data_source ds WHERE ds.id=@id::bigint)";
        return ExecuteAsync(
            db => db.ExecuteAsync<bool>(sql, cancellationToken, Parameter("id", id)),
            cancellationToken);
    }

    public Task<PageResult<RawDataDetailRecord>> QueryDetailsAsync(
        long sourceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        const string countSql = "SELECT count(*)::bigint FROM data_detail dd WHERE dd.source_id=@sourceId::bigint";
        var listSql = $"""
            SELECT {DetailColumns}
            FROM data_detail dd
            WHERE dd.source_id=@sourceId::bigint
            ORDER BY dd.create_time DESC, dd.id DESC
            LIMIT @limit::int OFFSET @offset::int
            """;

        return ExecuteAsync(async db =>
        {
            var total = await db.ExecuteAsync<long>(
                countSql,
                cancellationToken,
                Parameter("sourceId", sourceId));
            var items = await db.QueryToListAsync(
                ReadDetailList,
                listSql,
                cancellationToken,
                Parameter("sourceId", sourceId),
                Parameter("limit", normalizedPageSize),
                Parameter("offset", (normalizedPage - 1) * normalizedPageSize));

            return new PageResult<RawDataDetailRecord>(
                items,
                normalizedPage,
                normalizedPageSize,
                total);
        }, cancellationToken);
    }

    public Task<RawDataDetailRecord?> GetDetailAsync(long id, CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT {DetailColumns}, dd.raw
            FROM data_detail dd
            WHERE dd.id=@id::bigint
            """;

        return ExecuteAsync(async db =>
        {
            var rows = await db.QueryToListAsync(
                ReadDetailDetail,
                sql,
                cancellationToken,
                Parameter("id", id));
            return rows.Count > 0 ? rows[0] : null;
        }, cancellationToken);
    }

    private static RawDataSourceRecord ReadSourceList(DbDataReader reader)
    {
        return new RawDataSourceRecord(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetInt32(7),
            null,
            GetDateTime(reader, 8),
            GetDateTime(reader, 9));
    }

    private static RawDataSourceRecord ReadSourceDetail(DbDataReader reader)
    {
        return new RawDataSourceRecord(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetInt32(7),
            reader.IsDBNull(10) ? null : reader.GetFieldValue<byte[]>(10),
            GetDateTime(reader, 8),
            GetDateTime(reader, 9));
    }

    private static RawDataDetailRecord ReadDetailList(DbDataReader reader)
    {
        return new RawDataDetailRecord(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetInt32(7),
            null,
            GetDateTime(reader, 8),
            GetDateTime(reader, 9));
    }

    private static RawDataDetailRecord ReadDetailDetail(DbDataReader reader)
    {
        return new RawDataDetailRecord(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetInt32(7),
            reader.IsDBNull(10) ? null : reader.GetFieldValue<byte[]>(10),
            GetDateTime(reader, 8),
            GetDateTime(reader, 9));
    }
}
