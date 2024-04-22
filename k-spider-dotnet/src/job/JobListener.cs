using Quartz;
using SqlSugar;

namespace k_spider_dotnet.job;

public class JobListener(string name) : IJobListener
{
    public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        return Task.Run(() =>
        {

        });
    }

    // 任务拒绝执行时候的调用
    public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        
    }

    public Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? jobException,
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public string Name { get; set; } = name;
}