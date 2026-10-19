namespace KSpider.Job.Node;

/// <summary>
///     节点标识 : 状态与指令按节点区分 , v1 单节点用主机名。
///     分布式演进 : 每个爬虫进程一个标识 , spider_job_state / spider_job_command / spider_node_status
///     天然按节点分组 , Web 控制台按节点展示与路由。
/// </summary>
public static class NodeIdentity
{
    public static readonly string NodeId = Environment.MachineName;
}
