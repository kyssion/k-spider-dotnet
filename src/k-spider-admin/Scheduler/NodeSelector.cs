using KSpider.Admin.Tasks;
using KSpider.Admin.Tasks.Abstractions;

namespace KSpider.Admin.Scheduler;

/// <summary>
///     简单负载均衡：只在在线 crawler 节点中选当前在跑任务数最少的一个。
/// </summary>
/// <remarks>
///     device / ip / 端口明确不参与调度；同负载时按 node_id 排序保证结果稳定、可测试。
/// </remarks>
public sealed class NodeSelector
{
    /// <summary>
    ///     选择执行节点。
    /// </summary>
    /// <param name="nodes">候选节点快照。</param>
    /// <returns>选中的节点；没有 crawler 节点时返回 null。</returns>
    public NodeSnapshot? Select(IReadOnlyList<NodeSnapshot> nodes)
    {
        return nodes
            .Where(node => string.Equals(node.NodeType, NodeTypes.Crawler, StringComparison.Ordinal))
            .OrderBy(node => node.RunningTasks)
            .ThenBy(node => node.NodeId)
            .FirstOrDefault();
    }
}
