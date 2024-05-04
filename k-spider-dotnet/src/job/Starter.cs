using k_spider_dotnet.job.dfNewsJob;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;

namespace k_spider_dotnet.job;

public class Starter
{
    private const string NewsListGroup = "new_list_group";
    private static readonly StdSchedulerFactory SchedulerFactory = new();
    private readonly IScheduler _scheduler;

    public Starter()
    {
        _scheduler = SchedulerFactory.GetScheduler().Result;
        _scheduler.Start();
        _scheduler.ListenerManager.AddJobListener(new SpiderJobListener("StarterJobListener"),
            GroupMatcher<JobKey>.AnyGroup());
    }

    public void StartDfListNewsJob()
    {
        //调度器,生成实例的时候线程已经开启了，不过是在等待状态
        var jobBase = new DfNewsListJob();

        //创建一个Job,绑定MyJob
        var newJobDetail = jobBase.GetJobDetail(NewsListGroup);
        var newJobTrigger = jobBase.GetTrigger(NewsListGroup, newJobDetail);
        //start让调度线程启动【调度线程可以从jobstore中获取快要执行的trigger,然后获取trigger关联的job，执行job】
        //将job和trigger注册到scheduler中
        _scheduler.ScheduleJob(newJobDetail, newJobTrigger).Wait();
    }

    public void StartDfContentNewsJob()
    {
        //调度器,生成实例的时候线程已经开启了，不过是在等待状态
        var jobBase = new DfNewsContentJob();
        //创建一个Job,绑定MyJob
        var newJobDetail = jobBase.GetJobDetail(NewsListGroup);
        var newJobTrigger = jobBase.GetTrigger(NewsListGroup, newJobDetail);
        //start让调度线程启动【调度线程可以从jobstore中获取快要执行的trigger,然后获取trigger关联的job，执行job】
        //将job和trigger注册到scheduler中
        _scheduler.ScheduleJob(newJobDetail, newJobTrigger).Wait();
    }


    public void StartDfContentNewsOriginJob()
    {
        //调度器,生成实例的时候线程已经开启了，不过是在等待状态
        var jobBase = new DfNewsContentOriginJob();
        //创建一个Job,绑定MyJob
        var newJobDetail = jobBase.GetJobDetail(NewsListGroup);
        var newJobTrigger = jobBase.GetTrigger(NewsListGroup, newJobDetail);
        //start让调度线程启动【调度线程可以从jobstore中获取快要执行的trigger,然后获取trigger关联的job，执行job】
        //将job和trigger注册到scheduler中
        _scheduler.ScheduleJob(newJobDetail, newJobTrigger).Wait();
    }
}