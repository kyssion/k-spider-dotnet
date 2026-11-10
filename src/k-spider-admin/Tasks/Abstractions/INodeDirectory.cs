namespace KSpider.Admin.Tasks.Abstractions;

/// <summary>
///     在线节点目录。实现位于 Grpc，调度与对账只依赖本接口。
/// </summary>
public interface INodeDirectory
{
    /// <summary>
    ///     取指定类型的在线节点快照。
    /// </summary>
    /// <param name="nodeType">节点类型。</param>
    IReadOnlyList<NodeSnapshot> GetOnlineNodes(string nodeType);

    /// <summary>
    ///     读取单个节点快照。
    /// </summary>
    /// <param name="nodeId">节点标识。</param>
    /// <param name="snapshot">节点快照。</param>
    /// <returns>节点在线返回 true。</returns>
    bool TryGet(Guid nodeId, out NodeSnapshot snapshot);

    /// <summary>
    ///     从在线目录移除节点；关闭连接与写 offline 镜像由实现/调用方协调。
    /// </summary>
    /// <param name="nodeId">节点标识。</param>
    void MarkOffline(Guid nodeId);
}
