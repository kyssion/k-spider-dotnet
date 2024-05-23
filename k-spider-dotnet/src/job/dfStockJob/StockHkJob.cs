using k_spider_dotnet.dal.db;
using k_spider_dotnet.dao;
using k_spider_dotnet.model;
using k_spider_dotnet.script;
using k_spider_dotnet.script.df_stoke;
using k_spider_dotnet.script.df_stoke.china;
using k_spider_dotnet.script.df_stoke.hk;
using k_spider_dotnet.tool.log;
using Microsoft.Extensions.Logging;
using Quartz;

namespace k_spider_dotnet.job.dfStockJob;

public class StockHkJob : SpiderJob
{
    private const string JobName = "StockHkJob";
    private const string JobDescription = "港股信息同步";

    private static readonly ILogger Logger = LogFactory.GetLogger<StockHkJob>();
    private static readonly IStockSpider HkSpider = new HkStockSpider();

    public void SyncCnStock()
    {
        using var connection = Pg.Connection();
        var stockCnIntroductionList = connection.Queryable<StockCnIntroductionModel>()
            .Where(it=>it.ExchangeChannel == (int)StockExchangeChannel.HkStockExchangeChannel)
            .ToList();
        var date = DateTime.Today;
        foreach (var cnStockItem in stockCnIntroductionList)
        {
            var ans = "";
            try
            {
                ans =HkSpider.GetLevel1DailyArchived(cnStockItem.StockId??"").Result;
            }
            catch (Exception e)
            {
                Logger.LogError("[SyncCnStock] GetLevel1DailyArchived stock id : {}, Exception : {}" ,cnStockItem.StockId , e);
                continue;
            }
            try
            {
                StockDao.UpsetHkLevel1ArchivedDaily(connection, new StockHkLevel1ArchivedDailyOriginModel()
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
            .WithSimpleSchedule(x => x.WithIntervalInMinutes(2).RepeatForever().Build())
            .Build();
    }

    public override IJobDetail GetJobDetail(string jobGroup)
    {
        return JobBuilder.Create<StockHkJob>().WithIdentity(JobName + ".Job", jobGroup + ".Job")
            .DisallowConcurrentExecution() // 禁止并发执行
            .WithDescription(JobDescription).Build();
    }
}