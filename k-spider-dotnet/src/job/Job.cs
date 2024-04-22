using Quartz;
using Quartz.Impl;

namespace k_spider_dotnet.job;

public class MyJob : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        return Task.Run(() =>
        {
            Console.WriteLine("hello quartz!");
            //JobDetail的key就是job的分组和job的名字
            Console.WriteLine($"JobDetail的组和名字：{context.JobDetail.Key}");
            Console.WriteLine();
        });
    }
}

public class Job
{
    public static void StartDfListNewsJob()
    {
        //调度器,生成实例的时候线程已经开启了，不过是在等待状态
        StdSchedulerFactory factory = new StdSchedulerFactory();
        IScheduler scheduler = factory.GetScheduler().Result;

        //创建一个Job,绑定MyJob
        IJobDetail job = JobBuilder
            .Create<MyJob>() //获取JobBuilder
            .WithIdentity("jobname1", "group1") //添加Job的名字和分组
            .WithDescription("一个简单的任务") //添加描述
            .Build(); //生成IJobDetail

        //创建一个触发器
        ITrigger trigger =
            TriggerBuilder.Create() //获取TriggerBuilder
                .StartAt(DateBuilder.TodayAt(01, 00, 00)) //开始时间，今天的1点（hh,mm,ss），可使用StartNow()
                .ForJob(job) //将触发器关联给指定的job
                .WithPriority(10) //优先级，当触发时间一样时，优先级大的触发器先执行
                .WithIdentity("tname1", "group1") //添加名字和分组
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(1) //调度，一秒执行一次，执行三次
                    .WithRepeatCount(3)
                    .Build())
                .Build();

        //start让调度线程启动【调度线程可以从jobstore中获取快要执行的trigger,然后获取trigger关联的job，执行job】
        scheduler.Start();
        //将job和trigger注册到scheduler中
        scheduler.ScheduleJob(job, trigger).Wait();
        Console.ReadKey();
    }
}