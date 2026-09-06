using KSpider.Job;
using Quartz;

namespace KSpider.Sync.Job;

public class TransferSpiderDataJob : SpiderJob
{
    public override Task Execute(IJobExecutionContext context)
    {
        return Transfer.TransferSpiderData.DoTransfer();
    }
}
