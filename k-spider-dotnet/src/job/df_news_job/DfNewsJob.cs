using Quartz;

namespace k_spider_dotnet.job.df_news_job;

public class DfNewsJob : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        // todo 增加数据抓取脚本逻辑
        return Task.Run(() => { });
    }

}