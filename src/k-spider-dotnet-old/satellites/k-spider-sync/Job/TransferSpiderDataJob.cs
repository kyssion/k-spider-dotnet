using KSpider.Job;
using Quartz;

namespace KSpider.Sync.Job;

/// <summary>
///     数据搬运定时任务 : 把远端库的 5 张新闻表同步到本地 ( 调度注册在 Program , 执行体在 TransferSpiderData )
/// </summary>
public class TransferSpiderDataJob : SpiderJob
{
    public override Task Execute(IJobExecutionContext context)
    {
        return Transfer.TransferSpiderData.DoTransfer();
    }
}
