using System.Data.Common;
using KSpider.Admin.Repository.Abstractions;
using KSpider.Admin.Repository.Models;
using LinqToDB;
using LinqToDB.Data;

namespace KSpider.Admin.Repository.Postgres;

/// <summary>
///     sys_node 镜像数据访问；节点注册使用 ON CONFLICT 复活，心跳不经过本仓储。
/// </summary>
internal sealed class PostgresNodeRepository(IAdminDbConnectionFactory connectionFactory)
    : PostgresRepositoryBase(connectionFactory), ISysNodeRepository
{
    private const string NodeColumns =
        "id, node_id, node_type, status, ip, port, device::text AS device, extra::text AS extra, create_time, update_time";


    public Task UpsertOnlineAsync(SysNodeUpsert node, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO sys_node (node_id, node_type, status, ip, port, device, extra)
            VALUES (@nodeId::uuid, @nodeType::text, 'online', @ip::text, @port::int, @device::jsonb, @extra::jsonb)
            ON CONFLICT (node_id) DO UPDATE
            SET node_type=EXCLUDED.node_type,
                status='online',
                ip=EXCLUDED.ip,
                port=EXCLUDED.port,
                device=EXCLUDED.device,
                extra=EXCLUDED.extra,
                update_time=now()
            """;

        return ExecuteAsync(
            async db => await db.ExecuteAsync(
                sql,
                cancellationToken,
                Parameter("nodeId", node.NodeId),
                Parameter("nodeType", node.NodeType),
                Parameter("ip", node.Ip),
                Parameter("port", node.Port),
                Parameter("device", node.DeviceJson),
                Parameter("extra", node.ExtraJson)),
            cancellationToken);
    }

    public Task MarkOfflineAsync(Guid nodeId, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE sys_node
            SET status='offline', update_time=now()
            WHERE node_id=@nodeId::uuid
            """;

        return ExecuteAsync(
            async db => await db.ExecuteAsync(sql, cancellationToken, Parameter("nodeId", nodeId)),
            cancellationToken);
    }

    public Task<int> DeleteOfflineOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken)
    {
        const string sql = """
            DELETE FROM sys_node
            WHERE status='offline' AND update_time < @cutoff::timestamptz
            """;

        return ExecuteAsync(
            db => db.ExecuteAsync(sql, cancellationToken, Parameter("cutoff", cutoffUtc)),
            cancellationToken);
    }

    public Task<SysNodeRecord?> GetAsync(Guid nodeId, CancellationToken cancellationToken)
    {
        var sql = $"SELECT {NodeColumns} FROM sys_node WHERE node_id=@nodeId::uuid";
        return ExecuteAsync(async db =>
        {
            var rows = await db.QueryToListAsync(
                ReadNode,
                sql,
                cancellationToken,
                Parameter("nodeId", nodeId));
            return rows.Count > 0 ? rows[0] : null;
        }, cancellationToken);
    }

    public Task<IReadOnlyList<SysNodeRecord>> ListAsync(CancellationToken cancellationToken)
    {
        var sql = $"SELECT {NodeColumns} FROM sys_node ORDER BY node_id";
        return ExecuteAsync(
            async db => (IReadOnlyList<SysNodeRecord>)await db.QueryToListAsync(
                ReadNode,
                sql,
                cancellationToken),
            cancellationToken);
    }

    private static SysNodeRecord ReadNode(DbDataReader reader)
    {
        return new SysNodeRecord(
            reader.GetInt64(0),
            reader.GetGuid(1),
            reader.GetString(2),
            reader.GetString(3),
            GetNullableString(reader, 4),
            GetNullableInt32(reader, 5),
            GetNullableString(reader, 6),
            reader.GetString(7),
            GetDateTime(reader, 8),
            GetDateTime(reader, 9));
    }
}
