using Quartz;

namespace k_spider_dotnet_lib.job;

public abstract class SpiderJob : IJob
{
    public abstract Task Execute(IJobExecutionContext context);

    public abstract ITrigger GetTrigger(string jobGroup, IJobDetail jobDetail);
    public abstract IJobDetail GetJobDetail(string jobGroup);
}