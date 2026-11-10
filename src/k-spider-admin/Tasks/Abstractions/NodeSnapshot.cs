namespace KSpider.Admin.Tasks.Abstractions;

/// <summary>
///     admin 内存中的节点快照；心跳与在跑任务数不写库。
/// </summary>
/// <param name="NodeId">节点标识。</param>
/// <param name="NodeType">节点类型：crawler / process。</param>
/// <param name="RunningTasks">节点自报的在跑任务数；仅用于简单负载均衡。</param>
/// <param name="LastSeen">最后心跳时间；用于心跳超时判死。</param>
public sealed record NodeSnapshot(
    Guid NodeId,
    string NodeType,
    int RunningTasks,
    DateTimeOffset LastSeen);
