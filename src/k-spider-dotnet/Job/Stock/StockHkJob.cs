using KSpider.Data;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.DfStock;
using KSpider.Spider.DfStock.Hk;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace KSpider.Job.Stock;

public class StockHkJob(StockDao stockDao, Pg pg, ILogger<StockHkJob> logger) : SpiderJob
{
    private static readonly IStockSpider HkSpider = new HkStockSpider();

    public async Task SyncHkStock()
    {
        using var connection = pg.Connection();
        var stockCnIntroductionList = connection.Queryable<StockCnIntroductionModel>()
            .Where(it => it.ExchangeChannel == (int)StockExchangeChannel.HkStockExchangeChannel)
            .ToList();
        var date = DateTime.Today;
        foreach (var hkStockItem in stockCnIntroductionList)
        {
            var ans = "";
            try
            {
                ans = await HkSpider.GetLevel1DailyArchived(hkStockItem.StockId ?? "");
            }
            catch (Exception e)
            {
                logger.LogError("[SyncHkStock] GetLevel1DailyArchived stock id : {}, Exception : {}",
                    hkStockItem.StockId, e);
                continue;
            }

            // 当日无归档数据时跳过 , 不落空行
            if (string.IsNullOrEmpty(ans))
            {
                logger.LogWarning("[SyncHkStock] archived is empty , stock id : {}", hkStockItem.StockId);
                continue;
            }
            try
            {
                stockDao.UpsetHkLevel1ArchivedDaily(connection, new StockHkLevel1ArchivedDailyOriginModel
                {
                    StockId = hkStockItem.StockId ?? "",
                    ExchangeChannel = hkStockItem.ExchangeChannel ?? -1,
                    Date = date,
                    Archived = ans,
                    DataFrom = (int)FromTypeOfNews.DfMedia
                });
            }
            catch (Exception e)
            {
                logger.LogError("[SyncHkStock] UpsetHkLevel1ArchivedDaily stock id : {}, Exception : {}",
                    hkStockItem.StockId, e);
            }
        }
    }

    public override Task Execute(IJobExecutionContext context)
    {
        return SyncHkStock();
    }
}
