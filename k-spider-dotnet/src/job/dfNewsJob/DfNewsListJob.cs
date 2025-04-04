using k_spider_dotnet_lib.job;
using k_spider_dotnet_lib.logger;
using k_spider_dotnet.dal.db;
using k_spider_dotnet.dao;
using k_spider_dotnet.script;
using k_spider_dotnet.script.df_news.spider;
using Microsoft.Extensions.Logging;
using Quartz;

namespace k_spider_dotnet.job.dfNewsJob;

public class DfNewsListJob : SpiderJob
{
    private const string JobName = "DfNewsJob";
    private const string JobDescription = "东方财富网站抓取新闻列表信息任务";

    private static readonly ILogger Logger = LogFactory.GetLogger<DfNewsListJob>();
    private static readonly DfListSpider DfListSpider = new();

    public override Task Execute(IJobExecutionContext context)
    {
        return Task.Run(() =>
        {
            const int startNumber = 1;
            const int endNumber = 3;
            const int pageSize = 200;
            const DfListOrderType orderType = DfListOrderType.ByTime;
            using var connection = Pg.Connection();
            foreach (var resourceItem in DfResource.DfListUrlResourceList)
                try
                {
                    var dfListInfos = DfListSpider
                        .GetDfListInfoByUrl(resourceItem, startNumber, endNumber, pageSize, orderType).Result;
                    var dbDfListInfos = dfListInfos.Select(item => item.ToSpiderNewListModel())
                        .ToList();
                    SpiderNewsDao.UpsetSpiderNewsListInfo(connection, dbDfListInfos);
                }
                catch (Exception e)
                {
                    Logger.LogError("[DfNewsListJob Execute]  run error : {}", e);
                }
        });
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
        return JobBuilder.Create<DfNewsListJob>().WithIdentity(JobName + ".Job", jobGroup + ".Job")
            .DisallowConcurrentExecution() // 禁止并发执行
            .WithDescription(JobDescription).Build();
    }
}