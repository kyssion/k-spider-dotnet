namespace KSpider.Admin.Tasks;

/// <summary>
///     读取 sys_task.extra 的已知约定键；未知键由节点或其他模块维护，admin 不覆盖。
/// </summary>
public sealed record TaskExtraView
{
    /// <summary>本次派发 token；节点状态写入必须携带。</summary>
    public string? DispatchToken { get; init; }

    /// <summary>已重派次数；达到上限后转 abandoned。</summary>
    public int FailCount { get; init; }

    /// <summary>最近一次失败原因；成功重派时由仓储清除。</summary>
    public string? LastError { get; init; }
}
