using KSpider.Admin.Repository.Models;

namespace KSpider.Admin.Tasks;

/// <summary>
///     任务状态迁移结果。
/// </summary>
public enum TaskTransitionOutcome
{
    /// <summary>迁移已生效，或目标状态本来就是该状态。</summary>
    Applied,

    /// <summary>任务不存在。</summary>
    NotFound,

    /// <summary>状态或 token 已变化，调用方应按竞态处理。</summary>
    Conflict,

    /// <summary>当前状态不允许迁移到目标状态。</summary>
    InvalidTransition,

    /// <summary>达到最大失败次数，应转 abandoned。</summary>
    FailLimitReached,
}

/// <summary>
///     一次状态迁移的结果。
/// </summary>
/// <param name="Outcome">结果类型。</param>
/// <param name="Task">冲突时尽量带回当前任务记录，便于日志与诊断。</param>
/// <param name="Token">重派成功时返回新派发 token。</param>
public readonly record struct TaskTransitionResult(
    TaskTransitionOutcome Outcome,
    SysTaskRecord? Task = null,
    DispatchToken? Token = null)
{
    /// <summary>迁移是否已生效。</summary>
    public bool Applied => Outcome == TaskTransitionOutcome.Applied;
}
