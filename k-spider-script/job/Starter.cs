using k_spider_dotnet_lib.job;
using k_spider_dotnet_lib.logger;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;

namespace k_spider_script.job;

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
        _scheduler.ListenerManager.AddJobListener(new SpiderJobListener("StarterJobListener"),
            GroupMatcher<JobKey>.AnyGroup());
    }

    public void TransferSpiderDataJob()
    {
        //调度器,生成实例的时候线程已经开启了，不过是在等待状态
        var jobBase = new TransferSpiderDataJob();

        //创建一个Job,绑定MyJob
        var newJobDetail = jobBase.GetJobDetail(NewsListGroup);
        var newJobTrigger = jobBase.GetTrigger(NewsListGroup, newJobDetail);
        //start让调度线程启动【调度线程可以从jobstore中获取快要执行的trigger,然后获取trigger关联的job，执行job】
        //将job和trigger注册到scheduler中
        _scheduler.ScheduleJob(newJobDetail, newJobTrigger).Wait();
        Logger.LogInformation("[TransferSpiderDataJob] job is start , TransferSpiderDataJob");
    }
}