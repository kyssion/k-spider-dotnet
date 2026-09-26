using Quartz;

namespace KSpider.Job;

/// <summary>
///     全部定时任务的基类 , 调度注册集中写在 Program 的 AddSpiderJobs
/// </summary>
public abstract class SpiderJob : IJob
{
    /// <summary>
    ///     任务执行体 : 由 Quartz 触发 ; 网络调用全程 await , 不要 .Result / .Wait()
    /// </summary>
    public abstract Task Execute(IJobExecutionContext context);
}
