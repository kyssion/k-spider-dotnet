using System.Collections.Concurrent;
using Grpc.Core;
using KSpider.Admin.Tasks.Abstractions;
using GrpcScheduler = KSpider.Grpc.Scheduler;

namespace KSpider.Admin.Grpc;

/// <summary>
///     admin 内存节点花名册：在线判定与下行目标都来自这里，心跳不写库。
/// </summary>
/// <remarks>
///     <para>
///         同一 node_id 只保留最新连接；新连接注册时旧连接会 Close，旧流的 finally 通过引用比较
///         避免把新节点误写成 offline。
///     </para>
///     <para>
///         MarkOffline 会移除并关闭连接，返回的 snapshot 只反映调用瞬间，不持有连接引用。
///     </para>
/// </remarks>
/// <param name="timeProvider">可测试时钟；用于记录连接最后活跃时间。</param>
public sealed class NodeConnectionRegistry(TimeProvider timeProvider) : INodeDirectory
{
    private readonly ConcurrentDictionary<Guid, NodeConnection> _connections = new();

    /// <summary>
    ///     注册新连接；若同一节点已有连接，先替换再关闭旧连接。
    /// </summary>
    /// <param name="nodeId">节点标识。</param>
    /// <param name="nodeType">节点类型：crawler / process。</param>
    /// <param name="deviceJson">节点自报硬件 JSON，可为空。</param>
    /// <param name="writer">当前 gRPC 流的下行写入器。</param>
    /// <returns>新建立的连接对象。</returns>
    public NodeConnection Register(
        Guid nodeId,
        string nodeType,
        string? deviceJson,
        IServerStreamWriter<GrpcScheduler.AdminMessage> writer)
    {
        var connection = new NodeConnection(nodeId, nodeType, deviceJson, writer, timeProvider);

        while (true)
        {
            if (_connections.TryGetValue(nodeId, out var existing))
            {
                if (!_connections.TryUpdate(nodeId, connection, existing))
                {
                    continue;
                }

                existing.Close();
                return connection;
            }

            if (_connections.TryAdd(nodeId, connection))
            {
                return connection;
            }
        }
    }

    /// <summary>
    ///     仅当字典中仍是同一条连接时移除；避免旧流断开时删掉新连接。
    /// </summary>
    /// <param name="connection">要注销的连接对象。</param>
    public void Unregister(NodeConnection connection)
    {
        ((ICollection<KeyValuePair<Guid, NodeConnection>>)_connections).Remove(
            new KeyValuePair<Guid, NodeConnection>(connection.NodeId, connection));
    }

    /// <summary>
    ///     读取在线连接。
    /// </summary>
    /// <param name="nodeId">节点标识。</param>
    /// <param name="connection">找到的连接；不存在或已关闭时为 null。</param>
    /// <returns>是否存在可用连接。</returns>
    public bool TryGetConnection(Guid nodeId, out NodeConnection connection)
    {
        if (_connections.TryGetValue(nodeId, out var found) && !found.IsClosed)
        {
            connection = found;
            return true;
        }

        connection = null!;
        return false;
    }

    /// <summary>
    ///     取指定类型在线节点快照。
    /// </summary>
    /// <param name="nodeType">节点类型。</param>
    /// <returns>快照列表；调用方只读，不影响真实连接。</returns>
    public IReadOnlyList<NodeSnapshot> GetOnlineNodes(string nodeType)
    {
        return _connections.Values
            .Where(connection => !connection.IsClosed &&
                string.Equals(connection.NodeType, nodeType, StringComparison.Ordinal))
            .Select(connection => connection.Snapshot())
            .ToList();
    }

    /// <summary>
    ///     读取单个节点快照。
    /// </summary>
    /// <param name="nodeId">节点标识。</param>
    /// <param name="snapshot">节点快照；不存在时为 null。</param>
    /// <returns>是否存在在线连接。</returns>
    public bool TryGet(Guid nodeId, out NodeSnapshot snapshot)
    {
        if (TryGetConnection(nodeId, out var connection))
        {
            snapshot = connection.Snapshot();
            return true;
        }

        snapshot = null!;
        return false;
    }

    /// <summary>
    ///     移除并关闭节点连接；数据库 offline 镜像由调用方负责。
    /// </summary>
    /// <param name="nodeId">节点标识。</param>
    public void MarkOffline(Guid nodeId)
    {
        if (_connections.TryRemove(nodeId, out var connection))
        {
            connection.Close();
        }
    }
}
