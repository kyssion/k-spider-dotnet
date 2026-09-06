using k_spider_dotnet.job;
using k_spider_dotnet.logger;
using k_spider_sync.transfer;
using Microsoft.Extensions.Logging;
using Quartz;

namespace k_spider_sync.job;

public class TransferSpiderDataJob :SpiderJob
{
    private const string JobName = "TransferSpiderDataJob";
    private const string JobDescription = "同步spider排重新闻数据到本地";

    private static readonly ILogger Logger = LogFactory.GetLogger<TransferSpiderDataJob>();
    public override Task Execute(IJobExecutionContext context)
    {
        return TransferSpiderData.DoTransfer();
    }

    public override ITrigger GetTrigger(string jobGroup, IJobDetail jobDetail)
    {
        return TriggerBuilder.Create().ForJob(jobDetail)
            .WithIdentity(JobName + ".Trigger", jobGroup + ".Trigger").StartNow()
            .WithSimpleSchedule(x => x.WithIntervalInMinutes(2).RepeatForever().Build())
            .Build();
    }

    public override IJobDetail GetJobDetail(string jobGroup)
    {
        return JobBuilder.Create<TransferSpiderDataJob>().WithIdentity(JobName + ".Job", jobGroup + ".Job")
            .DisallowConcurrentExecution() // 禁止并发执行
            .WithDescription(JobDescription).Build();
    }
}