using k_spider_dotnet.tool.log;
using Microsoft.Extensions.Logging;
using Quartz;

namespace k_spider_dotnet.job;

public class SpiderJobListener(string name) : IJobListener
{
    private static readonly ILogger Log = LogFactory.GetLogger<SpiderJobListener>();

    // 任务开始执行
    public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = new())
    {
        return Task.Run(
            () =>
            {
                // Log.LogInformation("{namespace} to be executed , start time : {startTime}", context.JobDetail.JobType,
                //     DateTime.Now);
            }, cancellationToken);
    }

    // 任务拒绝执行时候的调用
    public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = new())
    {
        return Task.Run(
            () =>
            {
                // Log.LogInformation("{namespace} is vetoed , start time : {startTime}", context.JobDetail.JobType,
                //     DateTime.Now);
            }, cancellationToken);
    }

    // 任务完成之后
    public Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? jobException,
        CancellationToken cancellationToken = new())
    {
        return Task.Run(
            () =>
            {
                Log.LogInformation("{namespace} was executed , end time : {startTime}", context.JobDetail.JobType,
                    DateTime.Now);
            }, cancellationToken);
    }

    public string Name { get; set; } = name;
}