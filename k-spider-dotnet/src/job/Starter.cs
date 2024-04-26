using k_spider_dotnet.job.df_news_job;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;

namespace k_spider_dotnet.job;

public class Starter
{
    private const string NewsListGroup = "new_list_group";
    private static readonly StdSchedulerFactory SchedulerFactory = new StdSchedulerFactory();

    public static void StartDfListNewsJob()
    {
        //调度器,生成实例的时候线程已经开启了，不过是在等待状态
        var scheduler = SchedulerFactory.GetScheduler().Result;
        var dfNewJobBase = new DfNewsJob();
        
        //创建一个Job,绑定MyJob
        var newJobDetail = dfNewJobBase.GetJobDetail(NewsListGroup);
        var newJobTrigger = dfNewJobBase.GetTrigger(NewsListGroup,newJobDetail);
        //start让调度线程启动【调度线程可以从jobstore中获取快要执行的trigger,然后获取trigger关联的job，执行job】
        scheduler.Start();
        scheduler.ListenerManager.AddJobListener(new SpiderJobListener("df_job"), GroupMatcher<JobKey>.AnyGroup());
        //将job和trigger注册到scheduler中
        scheduler.ScheduleJob(newJobDetail,newJobTrigger).Wait();
        Console.ReadKey();
    }
}