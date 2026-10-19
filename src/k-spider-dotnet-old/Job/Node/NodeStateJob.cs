using KSpider.Data;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Matchers;
using SqlSugar;

namespace KSpider.Job.Node;

/// <summary>
///     节点状态任务 ( 3 秒 ) : 爬虫进程对 Web 控制台的两条通道都在这里 ——
///     1. 调度态上报 : 从 Quartz 调度器读各任务下次触发时间与暂停状态 , upsert 到 spider_job_state ;
///     2. 指令消费 : 轮询 spider_job_command 的 pending 指令并执行 ( 手动触发 / 暂停 / 恢复 )。
///     全程不向外抛异常 : 任一步失败只记日志 , 下一轮 ( 3 秒后 ) 自然重试。
///     分布式演进 : 多节点时各节点同跑本任务 , 靠指令表的 node_id 路由 ( 空值仍表示任意节点 )。
/// </summary>
public class NodeStateJob(ISchedulerFactory schedulerFactory, SystemStatusDao systemStatusDao, Pg pg,
    ILogger<NodeStateJob> logger) : SpiderJob
{
    /// <summary>任务名 : 注册与"不许暂停自己"的防误操作判断都用它</summary>
    public const string JobName = "NodeStateJob";

    public override async Task Execute(IJobExecutionContext context)
    {
        try
        {
            using var connection = pg.Connection();
            await RefreshJobSchedulesAsync(connection);
            await ConsumeCommandsAsync(connection);
        }
        catch (Exception e)
        {
            logger.LogError("[NodeStateJob Execute] err : {}", e);
        }
    }

    /// <summary>刷新全部任务的调度态 ( 下次触发时间 / 是否暂停 ) 到 spider_job_state</summary>
    private async Task RefreshJobSchedulesAsync(SqlSugarClient connection)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
        foreach (var jobKey in jobKeys)
        {
            // 一个任务可能挂多个触发器 : 下次触发时间取最早的 ; 任一触发器暂停即视为暂停
            var triggers = await scheduler.GetTriggersOfJob(jobKey);
            DateTimeOffset? nextFireTime = triggers.Count == 0
                ? null
                : triggers.Select(trigger => trigger.GetNextFireTimeUtc()).Min();
            var isPaused = false;
            foreach (var trigger in triggers)
                if (await scheduler.GetTriggerState(trigger.Key) == TriggerState.Paused)
                    isPaused = true;
            systemStatusDao.UpsertJobSchedule(connection, NodeIdentity.NodeId, jobKey.Name,
                nextFireTime?.LocalDateTime, isPaused);
        }
    }

    /// <summary>消费 pending 指令 ; 回写结果失败只记日志 ( 指令会重复执行一次 , 后果是多跑一轮任务 , 可接受 )</summary>
    private async Task ConsumeCommandsAsync(SqlSugarClient connection)
    {
        var commands = systemStatusDao.PendingJobCommands(connection, NodeIdentity.NodeId);
        if (commands.Count == 0) return;

        var scheduler = await schedulerFactory.GetScheduler();
        foreach (var command in commands)
        {
            // 状态通道任务自身不可暂停 : 它停了就没人消费"恢复"指令 , 节点会永远停在暂停态
            if (command.JobName == JobName && command.Action == "pause")
            {
                FinishSafely(connection, command.Id, "rejected", "不允许暂停状态通道任务 ( 它负责消费恢复指令 )");
                continue;
            }

            try
            {
                var jobKey = JobKey.Create(command.JobName);
                if (await scheduler.GetJobDetail(jobKey) == null)
                {
                    FinishSafely(connection, command.Id, "rejected", $"任务不存在 : {command.JobName}");
                    continue;
                }

                switch (command.Action)
                {
                    case "trigger":
                        await scheduler.TriggerJob(jobKey);
                        FinishSafely(connection, command.Id, "done", "已触发");
                        break;
                    case "pause":
                        await scheduler.PauseJob(jobKey);
                        FinishSafely(connection, command.Id, "done", "已暂停");
                        break;
                    case "resume":
                        await scheduler.ResumeJob(jobKey);
                        FinishSafely(connection, command.Id, "done", "已恢复");
                        break;
                    default:
                        FinishSafely(connection, command.Id, "rejected", $"未知指令类型 : {command.Action}");
                        continue;
                }

                logger.LogInformation(
                    "[NodeStateJob ConsumeCommandsAsync] command done , id : {} , job : {} , action : {}",
                    command.Id, command.JobName, command.Action);
            }
            catch (SchedulerException e)
            {
                FinishSafely(connection, command.Id, "rejected", $"指令执行失败 : {e.Message}");
            }
        }
    }

    /// <summary>回写指令结果 , 失败只记日志不中断后续指令</summary>
    private void FinishSafely(SqlSugarClient connection, long id, string status, string result)
    {
        try
        {
            systemStatusDao.FinishJobCommand(connection, id, status, result);
        }
        catch (Exception e)
        {
            logger.LogError("[NodeStateJob FinishSafely] err : {} , command id : {}", e, id);
        }
    }
}
