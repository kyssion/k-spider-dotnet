using KSpider.Admin.Api.Contracts;
using KSpider.Admin.Repository.Abstractions;
using KSpider.Admin.Repository.Models;

namespace KSpider.Admin.Api.Queries;

/// <summary>
///     sys_node 镜像只读查询服务。
/// </summary>
/// <remarks>
///     在线判定的实时事实源在 admin 内存注册表；这里返回的是数据库镜像，可能比内存状态
///     稍旧，适合控制台展示，不适合调度决策。
/// </remarks>
/// <param name="nodes">节点镜像仓储。</param>
public sealed class NodeQueryService(ISysNodeRepository nodes)
{
    /// <summary>
    ///     列出节点镜像。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<IReadOnlyList<NodeView>> ListAsync(CancellationToken cancellationToken)
    {
        var records = await nodes.ListAsync(cancellationToken);
        return records.Select(Map).ToList();
    }

    /// <summary>
    ///     读取单个节点镜像。
    /// </summary>
    /// <param name="nodeId">节点标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>节点视图；不存在时返回 null。</returns>
    public async Task<NodeView?> GetAsync(Guid nodeId, CancellationToken cancellationToken)
    {
        var node = await nodes.GetAsync(nodeId, cancellationToken);
        return node is null ? null : Map(node);
    }

    private static NodeView Map(SysNodeRecord node)
    {
        return new NodeView(
            node.NodeId,
            node.NodeType,
            node.Status,
            node.DeviceJson,
            node.ExtraJson,
            node.CreateTime,
            node.UpdateTime);
    }
}
