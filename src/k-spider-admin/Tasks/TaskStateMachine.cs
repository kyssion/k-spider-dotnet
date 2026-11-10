namespace KSpider.Admin.Tasks;

/// <summary>
///     状态迁移的执行方。
/// </summary>
public enum TransitionActor
{
    /// <summary>admin 控制面：推送系状态、取消、重派、放弃。</summary>
    Admin,

    /// <summary>执行节点：running、success、run_failed。</summary>
    Node,
}

/// <summary>
///     任务状态机。
/// </summary>
/// <remarks>
///     <para>
///         正常路径：pushing → pushed → running → success / run_failed。
///         节点也可能在 admin 写 pushed 之前直接进入 running，因此允许 pushing → running。
///     </para>
///     <para>
///         admin 的例外路径：push_failed / run_failed / pushed / running → pushing 属于重派；
///         其中 running → pushing 只允许对账在探测确认节点不可达后使用。
///         任意非终态 → cancelled、非终态失败系列 → abandoned。
///     </para>
///     <para>
///         本状态机只做“是否允许”的判断；真正的并发安全由仓储条件更新与 dispatch token 保证。
///     </para>
/// </remarks>
public static class TaskStateMachine
{
    /// <summary>
    ///     判断指定执行方能否把任务从 <paramref name="from" /> 迁到 <paramref name="to" />。
    /// </summary>
    /// <param name="from">当前状态。</param>
    /// <param name="to">目标状态。</param>
    /// <param name="actor">执行方。</param>
    public static bool CanTransition(AdminTaskStatus from, AdminTaskStatus to, TransitionActor actor)
    {
        return actor switch
        {
            TransitionActor.Admin => CanAdminTransition(from, to),
            TransitionActor.Node => CanNodeTransition(from, to),
            _ => false,
        };
    }

    private static bool CanAdminTransition(AdminTaskStatus from, AdminTaskStatus to)
    {
        if (to == AdminTaskStatus.Cancelled)
        {
            return !from.IsTerminal();
        }

        if (to == AdminTaskStatus.Abandoned)
        {
            return from is AdminTaskStatus.Pushing or AdminTaskStatus.PushFailed or AdminTaskStatus.RunFailed
                or AdminTaskStatus.Pushed or AdminTaskStatus.Running;
        }

        return (from, to) switch
        {
            (AdminTaskStatus.Pushing, AdminTaskStatus.Pushed) => true,
            (AdminTaskStatus.Pushing, AdminTaskStatus.PushFailed) => true,
            // 对账恢复崩溃前卡在 pushing 的任务：生成新 token 后重新下发。
            (AdminTaskStatus.Pushing, AdminTaskStatus.Pushing) => true,
            (AdminTaskStatus.PushFailed, AdminTaskStatus.Pushing) => true,
            (AdminTaskStatus.Pushed, AdminTaskStatus.Pushing) => true,
            (AdminTaskStatus.RunFailed, AdminTaskStatus.Pushing) => true,
            // 仅对账在探测确认节点不可达后使用：把死节点上的 running 任务重派。
            (AdminTaskStatus.Running, AdminTaskStatus.Pushing) => true,
            _ => false,
        };
    }

    private static bool CanNodeTransition(AdminTaskStatus from, AdminTaskStatus to)
    {
        return (from, to) switch
        {
            // 节点可能在 admin 写入 pushed 之前就收到 task_dispatch 并开始执行。
            (AdminTaskStatus.Pushing, AdminTaskStatus.Running) => true,
            (AdminTaskStatus.Pushed, AdminTaskStatus.Running) => true,
            (AdminTaskStatus.Running, AdminTaskStatus.Success) => true,
            (AdminTaskStatus.Running, AdminTaskStatus.RunFailed) => true,
            _ => false,
        };
    }
}
