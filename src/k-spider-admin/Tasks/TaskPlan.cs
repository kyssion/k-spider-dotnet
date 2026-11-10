using KSpider.Admin.Common.Json;

namespace KSpider.Admin.Tasks;

/// <summary>
///     调度器生成的一轮任务计划；节点在派发前由 NodeSelector 单独选择。
/// </summary>
/// <param name="ChannelId">渠道主键。</param>
/// <param name="TaskType">任务类型，如 channel_round。</param>
/// <param name="Detail">任务包内容；会序列化进 sys_task.task_detail。</param>
public sealed record TaskPlan(long ChannelId, string TaskType, TaskDetail Detail)
{
    /// <summary>
    ///     把任务包内容序列化为 JSONB 字符串。
    /// </summary>
    public string DetailJson()
    {
        return AdminJson.Serialize(Detail);
    }
}
