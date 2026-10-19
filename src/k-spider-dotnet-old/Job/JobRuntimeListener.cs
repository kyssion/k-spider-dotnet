using KSpider.Data;
using KSpider.Job.Node;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace KSpider.Job;

/// <summary>
///     任务执行结果上报监听器 : 每次任务执行完把 结果 / 耗时 / 异常 / 摘要 写 spider_job_state ,
///     供 k-spider-web 控制台跨进程展示。上报失败只记日志 , 绝不影响任务本身。
/// </summary>
public class JobRuntimeListener(Pg pg, SystemStatusDao systemStatusDao, ILogger<JobRuntimeListener> logger)
    : IJobListener
{
    /// <summary>执行开始时刻在本次执行上下文里的键 ( Quartz 3 无执行耗时属性 , 监听器自行打点 )</summary>
    private const string StartedAtKey = "JobRuntimeListener.StartedAt";

    public string Name => "JobRuntimeListener";

    public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        context.Put(StartedAtKey, DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }

    public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? jobException,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 取不到打点 ( 如进程内直接调 Execute 的测试 ) 退回触发时刻 , 耗时会略偏大但不会抛
            var startedAt = context.Get(StartedAtKey) as DateTimeOffset? ?? context.FireTimeUtc;
            using var connection = pg.Connection();
            systemStatusDao.UpsertJobExecution(connection, new JobExecutionReport(
                NodeIdentity.NodeId,
                context.JobDetail.Key.Name,
                context.FireTimeUtc.LocalDateTime,
                (long)(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds,
                jobException == null,
                jobException?.ToString(),
                (context.JobInstance as SpiderJob)?.RunSummary,
                context.NextFireTimeUtc?.LocalDateTime));
        }
        catch (Exception e)
        {
            logger.LogError("[JobRuntimeListener JobWasExecuted] 上报执行结果失败 , job : {Job} , err : {}",
                context.JobDetail.Key.Name, e);
        }
    }
}
