using Quartz;

namespace KSpider.Job;

/// <summary>
///     全部定时任务的基类 , 调度注册集中写在 Program 的 AddSpiderJobs
/// </summary>
public abstract class SpiderJob : IJob
{
    /// <summary>
    ///     本轮执行摘要 ( 可选 , 如 "新增列表 12" ) : 在 Execute 末尾赋值 ,
    ///     由 JobRuntimeListener 上报到 spider_job_state.last_stats 供控制台展示 , 不设就是空。
    /// </summary>
    public string? RunSummary { get; protected set; }

    /// <summary>
    ///     任务执行体 : 由 Quartz 触发 ; 网络调用全程 await , 不要 .Result / .Wait()
    /// </summary>
    public abstract Task Execute(IJobExecutionContext context);
}
