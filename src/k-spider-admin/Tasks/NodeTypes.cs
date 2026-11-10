namespace KSpider.Admin.Tasks;

/// <summary>
///     sys_node.node_type 的取值。
/// </summary>
public static class NodeTypes
{
    /// <summary>admin 控制面自身。</summary>
    public const string Admin = "admin";
    /// <summary>抓取节点。</summary>
    public const string Crawler = "crawler";
    /// <summary>数据处理节点（预留）。</summary>
    public const string Process = "process";
}
