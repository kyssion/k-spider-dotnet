using k_spider_dotnet.data;
using k_spider_dotnet.job.check;
using k_spider_dotnet.job.news;
using k_spider_dotnet.job.stock;
using k_spider_dotnet.logger;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;

namespace k_spider_dotnet.job;

public class Starter
{
    private const string NewsListGroup = "k-spider-news-group";
    private static readonly StdSchedulerFactory SchedulerFactory = new();
    private static readonly ILogger Logger = LogFactory.GetLogger<Starter>();
    private readonly IScheduler _scheduler;

    public Starter()
    {
        _scheduler = SchedulerFactory.GetScheduler().Result;
        _scheduler.Start();
        Pg.EnsureSpiderNewsListDbObjects();
    }

    /// <summary>
    ///     优雅停机 : 等待正在执行的任务完成后关闭调度器
    /// </summary>
    public void Shutdown()
    {
        _scheduler.Shutdown(true).Wait();
        Logger.LogInformation("[Shutdown] scheduler is shutdown");
    }

    /// <summary>
    ///     统一的任务注册 : 生成 JobDetail 与 Trigger 并注册到调度器 ( 调度线程在构造时已启动 )
    /// </summary>
    private void ScheduleJob(SpiderJob job)
    {
        var jobDetail = job.GetJobDetail(NewsListGroup);
        var trigger = job.GetTrigger(NewsListGroup, jobDetail);
        _scheduler.ScheduleJob(jobDetail, trigger).Wait();
        Logger.LogInformation("[Start{JobName}] job is start", job.GetType().Name);
    }

    public void StartDfListNewsJob() => ScheduleJob(new DfNewsListJob());

    public void StartDfContentNewsJob() => ScheduleJob(new DfNewsContentJob());

    public void StartDfContentNewsOriginJob() => ScheduleJob(new DfNewsContentOriginJob());

    public void StartCheckJob() => ScheduleJob(new DfCheckJob());

    public void StartStockCnJob() => ScheduleJob(new StockCnJob());

    public void StartStockHkJob() => ScheduleJob(new StockHkJob());
}
