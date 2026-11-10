namespace KSpider.Admin.Tasks;

/// <summary>
///     sys_task.status 的类型化表示；数据库仍存 text，映射集中在本文件。
/// </summary>
public enum AdminTaskStatus
{
    /// <summary>任务已落库，等待 admin 下发。</summary>
    Pushing,

    /// <summary>下发失败，等待对账重派或放弃。</summary>
    PushFailed,

    /// <summary>已写入节点流，等待节点接手。</summary>
    Pushed,

    /// <summary>节点执行中；由节点直写并持续 touch update_time。</summary>
    Running,

    /// <summary>本轮执行失败；由节点直写，等待 admin 重派。</summary>
    RunFailed,

    /// <summary>终态：执行成功。</summary>
    Success,

    /// <summary>终态：重派达到上限，admin 放弃。</summary>
    Abandoned,

    /// <summary>终态：渠道暂停或人工取消。</summary>
    Cancelled,
}

/// <summary>
///     任务状态与数据库 text 值之间的唯一映射。
/// </summary>
public static class AdminTaskStatusExtensions
{
    /// <summary>
    ///     转为 sys_task.status 的数据库值。
    /// </summary>
    /// <param name="status">任务状态。</param>
    /// <exception cref="ArgumentOutOfRangeException">未知枚举值。</exception>
    public static string ToDbValue(this AdminTaskStatus status)
    {
        return status switch
        {
            AdminTaskStatus.Pushing => "pushing",
            AdminTaskStatus.PushFailed => "push_failed",
            AdminTaskStatus.Pushed => "pushed",
            AdminTaskStatus.Running => "running",
            AdminTaskStatus.RunFailed => "run_failed",
            AdminTaskStatus.Success => "success",
            AdminTaskStatus.Abandoned => "abandoned",
            AdminTaskStatus.Cancelled => "cancelled",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "未知任务状态"),
        };
    }

    /// <summary>
    ///     从数据库 text 解析任务状态。
    /// </summary>
    /// <param name="value">数据库值。</param>
    /// <param name="status">解析结果；失败时为默认值。</param>
    /// <returns>解析成功返回 true。</returns>
    public static bool TryParse(string? value, out AdminTaskStatus status)
    {
        switch (value)
        {
            case "pushing":
                status = AdminTaskStatus.Pushing;
                return true;
            case "push_failed":
                status = AdminTaskStatus.PushFailed;
                return true;
            case "pushed":
                status = AdminTaskStatus.Pushed;
                return true;
            case "running":
                status = AdminTaskStatus.Running;
                return true;
            case "run_failed":
                status = AdminTaskStatus.RunFailed;
                return true;
            case "success":
                status = AdminTaskStatus.Success;
                return true;
            case "abandoned":
                status = AdminTaskStatus.Abandoned;
                return true;
            case "cancelled":
                status = AdminTaskStatus.Cancelled;
                return true;
            default:
                status = default;
                return false;
        }
    }

    /// <summary>
    ///     是否为终态；终态不再参与调度、对账与重派。
    /// </summary>
    /// <param name="status">任务状态。</param>
    public static bool IsTerminal(this AdminTaskStatus status)
    {
        return status is AdminTaskStatus.Success or AdminTaskStatus.Abandoned or AdminTaskStatus.Cancelled;
    }
}
