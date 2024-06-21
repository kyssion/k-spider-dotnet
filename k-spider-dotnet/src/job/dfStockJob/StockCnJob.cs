using k_spider_dotnet_lib.logger;
using k_spider_dotnet.dal.db;
using k_spider_dotnet.dao;
using k_spider_dotnet.job.dfNewsJob;
using k_spider_dotnet.model;
using k_spider_dotnet.script;
using k_spider_dotnet.script.df_news.spider;
using k_spider_dotnet.script.df_stoke.china;
using Microsoft.Extensions.Logging;
using Quartz;

namespace k_spider_dotnet.job.dfStockJob;

public class StockCnJob : SpiderJob
{
    private const string JobName = "StockCnJob";
    private const string JobDescription = "A股信息同步";

    private static readonly ILogger Logger = LogFactory.GetLogger<StockCnJob>();
    private static readonly ChinaStockSpider ShangHSpider = new ShStockSpider();
    private static readonly ChinaStockSpider ShenZSpider = new SzBjStockSpider();

    public void SyncCnStock()
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
                        ans = ShenZSpider.GetLevel1DailyArchived(cnStockItem.StockId??"").Result;
                        break;
                    case (int)StockExchangeChannel.ShangHStockExchangeChannel:
                        ans = ShangHSpider.GetLevel1DailyArchived(cnStockItem.StockId??"").Result;
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
        return Task.Run(SyncCnStock);
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