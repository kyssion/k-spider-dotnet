using KSpider.Admin.Common.Background;
using KSpider.Admin.Common.Logging;
using KSpider.Admin.Repository.Abstractions;
using KSpider.Admin.Tasks;
using KSpider.Admin.Tasks.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KSpider.Admin.Reconciliation;

/// <summary>
///     心跳超时检测：admin 内存是事实源；超时后置 sys_node 墓碑并立即重派名下任务。
/// </summary>
/// <remarks>
///     心跳不写库，因此本循环只看内存快照。节点被判定超时后会先从注册表移除并关闭连接，
///     再写数据库 offline 墓碑；随后立即触发任务重派，不必等待对账的常规超时窗口。
/// </remarks>
/// <param name="nodeDirectory">在线节点目录。</param>
/// <param name="nodes">sys_node 镜像仓储。</param>
/// <param name="reconciliation">对账服务；用于立即重派节点名下任务。</param>
/// <param name="options">心跳与对账周期配置。</param>
/// <param name="timeProvider">可测试时钟。</param>
/// <param name="logger">结构化日志。</param>
public sealed class NodeHealthMonitor(
    INodeDirectory nodeDirectory,
    ISysNodeRepository nodes,
    IReconciliationService reconciliation,
    IOptions<ReconciliationOptions> options,
    TimeProvider timeProvider,
    ILogger<NodeHealthMonitor> logger)
    : PeriodicBackgroundService(TimeSpan.FromSeconds(options.Value.TickSeconds), logger)
{
    private readonly ReconciliationOptions _options = options.Value;

    /// <summary>
    ///     执行一轮节点健康检查。
    /// </summary>
    /// <param name="cancellationToken">宿主停止令牌。</param>
    protected override async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var timeout = TimeSpan.FromSeconds(_options.HeartbeatTimeoutSeconds);
        var nodesSnapshot = nodeDirectory.GetOnlineNodes(NodeTypes.Crawler)
            .Concat(nodeDirectory.GetOnlineNodes(NodeTypes.Process))
            .ToList();

        foreach (var node in nodesSnapshot)
        {
            if (now - node.LastSeen <= timeout)
            {
                continue;
            }

            // 先移除内存连接，避免对账继续把任务派回死节点。
            nodeDirectory.MarkOffline(node.NodeId);
            await nodes.MarkOfflineAsync(node.NodeId, cancellationToken);
            AdminLog.NodeOffline(logger, node.NodeId, node.LastSeen);
            await reconciliation.RequeueNodeTasksAsync(node.NodeId, cancellationToken);
        }
    }
}
