using k_spider_dotnet.job;
using k_spider_dotnet.logger;
using k_spider_dotnet.data;
using k_spider_dotnet.model;
using k_spider_dotnet.spider;
using k_spider_dotnet.spider.df_stock;
using k_spider_dotnet.spider.df_stock.china;
using k_spider_dotnet.spider.df_stock.hk;
using Microsoft.Extensions.Logging;
using Quartz;

namespace k_spider_dotnet.job.stock;

public class StockHkJob : SpiderJob
{
    private const string JobName = "StockHkJob";
    private const string JobDescription = "港股信息同步";

    private static readonly ILogger Logger = LogFactory.GetLogger<StockHkJob>();
    private static readonly IStockSpider HkSpider = new HkStockSpider();

    public async Task SyncHkStock()
    {
        using var connection = Pg.Connection();
        var stockCnIntroductionList = connection.Queryable<StockCnIntroductionModel>()
            .Where(it=>it.ExchangeChannel == (int)StockExchangeChannel.HkStockExchangeChannel)
            .ToList();
        var date = DateTime.Today;
        foreach (var hkStockItem in stockCnIntroductionList)
        {
            var ans = "";
            try
            {
                ans = await HkSpider.GetLevel1DailyArchived(hkStockItem.StockId??"");
            }
            catch (Exception e)
            {
                Logger.LogError("[SyncHkStock] GetLevel1DailyArchived stock id : {}, Exception : {}" ,hkStockItem.StockId , e);
                continue;
            }
            // 当日无归档数据时跳过 , 不落空行
            if (string.IsNullOrEmpty(ans))
            {
                Logger.LogWarning("[SyncHkStock] archived is empty , stock id : {}", hkStockItem.StockId);
                continue;
            }
            try
            {
                StockDao.UpsetHkLevel1ArchivedDaily(connection, new StockHkLevel1ArchivedDailyOriginModel()
                {
                    StockId = hkStockItem.StockId??"",
                    ExchangeChannel = hkStockItem.ExchangeChannel??-1,
                    Date = date,
                    Archived = ans,
                    DataFrom = (int)FromTypeOfNews.DfMedia
                });
            }
            catch (Exception e)
            {
                Logger.LogError("[SyncHkStock] UpsetHkLevel1ArchivedDaily stock id : {}, Exception : {}" ,hkStockItem.StockId , e);
            }
        }
    }

    public override Task Execute(IJobExecutionContext context)
    {
        return SyncHkStock();
    }

    public override ITrigger GetTrigger(string jobGroup, IJobDetail jobDetail)
    {
        return TriggerBuilder.Create().ForJob(jobDetail)
            .WithIdentity(JobName + ".Trigger", jobGroup + ".Trigger").StartNow()
            // .WithSimpleSchedule(x => x.WithIntervalInMinutes(2).RepeatForever().Build())
            // .WithCronSchedule("0 0 13 * * ?") 
            .WithCronSchedule("0 0 20 ? * MON-FRI") // 每周一到周五晚上8点执行
            .Build();
    }

    public override IJobDetail GetJobDetail(string jobGroup)
    {
        return JobBuilder.Create<StockHkJob>().WithIdentity(JobName + ".Job", jobGroup + ".Job")
            .DisallowConcurrentExecution() // 禁止并发执行
            .WithDescription(JobDescription).Build();
    }
}
