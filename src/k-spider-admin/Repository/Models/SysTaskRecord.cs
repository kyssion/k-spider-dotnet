namespace KSpider.Admin.Repository.Models;

/// <summary>
///     sys_task 行快照。
/// </summary>
/// <param name="Id">任务主键。</param>
/// <param name="NodeId">执行节点 uuid。</param>
/// <param name="TaskType">任务类型。</param>
/// <param name="TaskDetailJson">task_detail JSON 文本。</param>
/// <param name="Status">任务状态文本。</param>
/// <param name="ExtraJson">extra JSON 文本。</param>
/// <param name="CreateTime">创建时间。</param>
/// <param name="UpdateTime">更新时间；对账判据。</param>
public sealed record SysTaskRecord(
    long Id,
    Guid NodeId,
    string TaskType,
    string TaskDetailJson,
    string Status,
    string ExtraJson,
    DateTime CreateTime,
    DateTime UpdateTime);

/// <summary>
///     任务插入参数。
/// </summary>
/// <param name="NodeId">目标节点。</param>
/// <param name="TaskType">任务类型。</param>
/// <param name="TaskDetailJson">任务包 JSON。</param>
/// <param name="Status">初始状态，应为 pushing。</param>
/// <param name="ExtraJson">初始 extra JSON。</param>
public sealed record SysTaskInsert(
    Guid NodeId,
    string TaskType,
    string TaskDetailJson,
    string Status,
    string ExtraJson);

/// <summary>
///     任务查询条件。
/// </summary>
/// <param name="Page">页码，从 1 开始。</param>
/// <param name="PageSize">每页数量，仓储会钳制到 1–100。</param>
/// <param name="Status">可选状态筛选。</param>
/// <param name="NodeId">可选节点筛选。</param>
public sealed record TaskQuery(int Page, int PageSize, string? Status = null, Guid? NodeId = null);
