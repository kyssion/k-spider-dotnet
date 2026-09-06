using k_spider_dotnet.logger;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;

namespace k_spider_sync.job;

public class Starter
{
    private const string NewsListGroup = "k-spider-script-group";
    private static readonly StdSchedulerFactory SchedulerFactory = new();
    private static readonly ILogger Logger = LogFactory.GetLogger<Starter>();
    private readonly IScheduler _scheduler;

    public Starter()
    {
        _scheduler = SchedulerFactory.GetScheduler().Result;
        _scheduler.Start();
    }

    /// <summary>
    ///     优雅停机 : 等待正在执行的任务完成后关闭调度器
    /// </summary>
    public void Shutdown()
    {
        _scheduler.Shutdown(true).Wait();
        Logger.LogInformation("[Shutdown] scheduler is shutdown");
    }

    public void TransferSpiderDataJob()
    {
        var jobBase = new TransferSpiderDataJob();
        var newJobDetail = jobBase.GetJobDetail(NewsListGroup);
        var newJobTrigger = jobBase.GetTrigger(NewsListGroup, newJobDetail);
        _scheduler.ScheduleJob(newJobDetail, newJobTrigger).Wait();
        Logger.LogInformation("[TransferSpiderDataJob] job is start , TransferSpiderDataJob");
    }
}
