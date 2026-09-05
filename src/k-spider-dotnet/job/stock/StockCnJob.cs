using k_spider_dotnet_lib.job;
using k_spider_dotnet_lib.logger;
using k_spider_dotnet.data;
using k_spider_dotnet.data;
using k_spider_dotnet.job.news;
using k_spider_dotnet.model;
using k_spider_dotnet.spider;
using k_spider_dotnet.spider.df_news;
using k_spider_dotnet.spider.df_stock.china;
using Microsoft.Extensions.Logging;
using Quartz;

namespace k_spider_dotnet.job.stock;

public class StockCnJob : SpiderJob
{
    private const string JobName = "StockCnJob";
    private const string JobDescription = "A股信息同步";

    private static readonly ILogger Logger = LogFactory.GetLogger<StockCnJob>();
    private static readonly ChinaStockSpider ShangHSpider = new ShStockSpider();
    private static readonly ChinaStockSpider ShenZSpider = new SzBjStockSpider();

    public async Task SyncCnStock()
    {
        using var connection = Pg.Connection();
        var stockCnIntroductionList = connection.Queryable<StockCnIntroductionModel>()
            .Where(it=>it.ExchangeChannel ==(int) StockExchangeChannel.SzBjStockExchangeChannel ||
                       it.ExchangeChannel == (int)StockExchangeChannel.ShangHStockExchangeChannel)
            .ToList();
        var date = DateTime.Today;
        foreach (var cnStockItem in stockCnIntroductionList)
        {
            var ans = "";
            try
            {
                switch (cnStockItem.ExchangeChannel)
                {
                    case (int)StockExchangeChannel.SzBjStockExchangeChannel:
                        ans = await ShenZSpider.GetLevel1DailyArchived(cnStockItem.StockId??"");
                        break;
                    case (int)StockExchangeChannel.ShangHStockExchangeChannel:
                        ans = await ShangHSpider.GetLevel1DailyArchived(cnStockItem.StockId??"");
                        break;
                    default:
                        Logger.LogError("[SyncCnStock] ExchangeChannel not find : {}", cnStockItem.ExchangeChannel);
                        continue;
                }
            }
            catch (Exception e)
            {
                Logger.LogError("[SyncCnStock] GetLevel1DailyArchived stock id : {}, Exception : {}" ,cnStockItem.StockId , e);
                continue;
            }
            // 当日无归档数据时跳过 , 不落空行
            if (string.IsNullOrEmpty(ans))
            {
                Logger.LogWarning("[SyncCnStock] archived is empty , stock id : {}", cnStockItem.StockId);
                continue;
            }
            try
            {
                StockDao.UpsetCnLevel1ArchivedDaily(connection, new StockCnLevel1ArchivedDailyOriginModel
                {
                    StockId = cnStockItem.StockId??"",
                    ExchangeChannel = cnStockItem.ExchangeChannel??-1,
                    Date = date,
                    Archived = ans,
                    DataFrom = (int)FromTypeOfNews.DfMedia
                });
            }
            catch (Exception e)
            {
                Logger.LogError("[SyncCnStock] UpsetCnLevel1ArchivedDaily stock id : {}, Exception : {}" ,cnStockItem.StockId , e);
            }
        }
    }

    public override Task Execute(IJobExecutionContext context)
    {
        return SyncCnStock();
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
        return JobBuilder.Create<StockCnJob>().WithIdentity(JobName + ".Job", jobGroup + ".Job")
            .DisallowConcurrentExecution() // 禁止并发执行
            .WithDescription(JobDescription).Build();
    }
}