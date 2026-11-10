namespace KSpider.Admin.Tasks;

/// <summary>
///     任务类型常量。
/// </summary>
public static class TaskTypes
{
    /// <summary>渠道轮任务；具体轮次类型放在 task_detail.round_type。</summary>
    public const string ChannelRound = "channel_round";
}

/// <summary>
///     渠道轮次类型常量。
/// </summary>
public static class RoundTypes
{
    /// <summary>列表轮。</summary>
    public const string List = "list";

    /// <summary>详情回填轮；admin 当前尚未生成。</summary>
    public const string Detail = "detail";
}

/// <summary>
///     sys_task.task_detail 的 admin 侧结构；序列化键名由 AdminJson 统一为 snake_case。
/// </summary>
/// <param name="ChannelId">渠道主键；必须与任务计划一致。</param>
/// <param name="RoundType">轮次类型：list / detail。</param>
/// <param name="BatchLimit">本轮批量上限，必须为正数。</param>
/// <param name="WindowStart">可选时间窗起点；列表轮通常为空。</param>
/// <param name="WindowEnd">可选时间窗终点；列表轮通常为空。</param>
public sealed record TaskDetail(
    long ChannelId,
    string RoundType,
    int BatchLimit,
    DateTimeOffset? WindowStart = null,
    DateTimeOffset? WindowEnd = null);
