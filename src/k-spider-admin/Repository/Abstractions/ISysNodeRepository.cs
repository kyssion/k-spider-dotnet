using KSpider.Admin.Repository.Models;

namespace KSpider.Admin.Repository.Abstractions;

/// <summary>
///     sys_node 镜像数据访问边界。
/// </summary>
/// <remarks>
///     在线判定的实时事实源是 admin 内存注册表；本仓储只维护数据库镜像，供控制台展示、
///     重启后审计与离线墓碑清理。心跳不经过这里。
/// </remarks>
public interface ISysNodeRepository
{
    /// <summary>
    ///     注册或复活节点：按 node_id upsert 为 online。
    /// </summary>
    /// <param name="node">节点身份、类型与展示信息。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task UpsertOnlineAsync(SysNodeUpsert node, CancellationToken cancellationToken);

    /// <summary>
    ///     把节点写成 offline 墓碑；行由清理循环稍后删除。
    /// </summary>
    /// <param name="nodeId">节点标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task MarkOfflineAsync(Guid nodeId, CancellationToken cancellationToken);

    /// <summary>
    ///     删除超过保留期的 offline 墓碑。
    /// </summary>
    /// <param name="cutoffUtc">update_time 截止时间（UTC）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>实际删除行数。</returns>
    Task<int> DeleteOfflineOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken);

    /// <summary>
    ///     按 node_id 读取节点镜像。
    /// </summary>
    /// <param name="nodeId">节点标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<SysNodeRecord?> GetAsync(Guid nodeId, CancellationToken cancellationToken);

    /// <summary>
    ///     列出全部节点镜像。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<IReadOnlyList<SysNodeRecord>> ListAsync(CancellationToken cancellationToken);
}
