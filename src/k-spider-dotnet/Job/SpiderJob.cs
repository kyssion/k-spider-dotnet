using Quartz;

namespace KSpider.Job;

/// <summary>
///     全部定时任务的基类 , 调度注册集中写在 Program 的 AddSpiderJobs
/// </summary>
public abstract class SpiderJob : IJob
{
    public abstract Task Execute(IJobExecutionContext context);
}
