using KSpider.Admin.Repository.Models;
using KSpider.Admin.Tasks;
using Microsoft.Extensions.Options;

namespace KSpider.Admin.Scheduler;

/// <summary>
///     把渠道配置转换为任务计划。
/// </summary>
/// <remarks>
///     当前只生成列表轮；详情回填轮与平台任务随 crawler/process 实现补入。
///     batch_limit 暂用全局默认值，渠道级覆盖在 channel.extra 键表定稿后接入。
/// </remarks>
/// <param name="options">调度配置。</param>
public sealed class TaskPlanner(IOptions<SchedulerOptions> options)
{
    private readonly SchedulerOptions _options = options.Value;

    /// <summary>
    ///     创建列表轮任务计划。
    /// </summary>
    /// <param name="channel">已启用的渠道记录。</param>
    /// <returns>可直接交给 admin 任务生命周期创建的任务计划。</returns>
    public TaskPlan CreateListRound(ChannelRecord channel)
    {
        var detail = new TaskDetail(
            channel.Id,
            RoundTypes.List,
            _options.DefaultBatchLimit);

        return new TaskPlan(channel.Id, TaskTypes.ChannelRound, detail);
    }
}
