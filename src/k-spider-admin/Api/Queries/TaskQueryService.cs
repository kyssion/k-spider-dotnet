using KSpider.Admin.Api.Contracts;
using KSpider.Admin.Repository.Abstractions;
using KSpider.Admin.Repository.Models;

namespace KSpider.Admin.Api.Queries;

/// <summary>
///     任务只读查询服务。
/// </summary>
/// <remarks>
///     返回 task_detail / extra 原始 JSON 字符串，由前端按任务类型解析；admin 不在查询层
///     猜测任务类型的业务字段。
/// </remarks>
/// <param name="tasks">任务仓储。</param>
public sealed class TaskQueryService(ITaskRepository tasks)
{
    /// <summary>
    ///     分页查询任务。
    /// </summary>
    /// <param name="query">筛选与分页条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<PageResponse<TaskView>> QueryAsync(TaskQuery query, CancellationToken cancellationToken)
    {
        var page = await tasks.QueryAsync(query, cancellationToken);
        return new PageResponse<TaskView>(
            page.Items.Select(Map).ToList(),
            page.Page,
            page.PageSize,
            page.Total);
    }

    /// <summary>
    ///     读取单个任务。
    /// </summary>
    /// <param name="id">任务主键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>任务视图；不存在时返回 null。</returns>
    public async Task<TaskView?> GetAsync(long id, CancellationToken cancellationToken)
    {
        var task = await tasks.GetAsync(id, cancellationToken);
        return task is null ? null : Map(task);
    }

    private static TaskView Map(SysTaskRecord task)
    {
        return new TaskView(
            task.Id,
            task.NodeId,
            task.TaskType,
            task.TaskDetailJson,
            task.Status,
            task.ExtraJson,
            task.CreateTime,
            task.UpdateTime);
    }
}
