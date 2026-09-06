using KSpider.Data;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.DfStock.China;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace KSpider.Job.Stock;

public class StockCnJob(StockDao stockDao, Pg pg, ILogger<StockCnJob> logger) : SpiderJob
{
    private static readonly ChinaStockSpider ShangHSpider = new ShStockSpider();
    private static readonly ChinaStockSpider ShenZSpider = new SzBjStockSpider();

    public async Task SyncCnStock()
    {
        using var connection = pg.Connection();
        var stockCnIntroductionList = connection.Queryable<StockCnIntroductionModel>()
            .Where(it => it.ExchangeChannel == (int)StockExchangeChannel.SzBjStockExchangeChannel ||
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
                        ans = await ShenZSpider.GetLevel1DailyArchived(cnStockItem.StockId ?? "");
                        break;
                    case (int)StockExchangeChannel.ShangHStockExchangeChannel:
                        ans = await ShangHSpider.GetLevel1DailyArchived(cnStockItem.StockId ?? "");
                        break;
                    default:
                        logger.LogError("[SyncCnStock] ExchangeChannel not find : {}", cnStockItem.ExchangeChannel);
                        continue;
                }
            }
            catch (Exception e)
            {
                logger.LogError("[SyncCnStock] GetLevel1DailyArchived stock id : {}, Exception : {}",
                    cnStockItem.StockId, e);
                continue;
            }

            // 当日无归档数据时跳过 , 不落空行
            if (string.IsNullOrEmpty(ans))
            {
                logger.LogWarning("[SyncCnStock] archived is empty , stock id : {}", cnStockItem.StockId);
                continue;
            }
            try
            {
                stockDao.UpsetCnLevel1ArchivedDaily(connection, new StockCnLevel1ArchivedDailyOriginModel
                {
                    StockId = cnStockItem.StockId ?? "",
                    ExchangeChannel = cnStockItem.ExchangeChannel ?? -1,
                    Date = date,
                    Archived = ans,
                    DataFrom = (int)FromTypeOfNews.DfMedia
                });
            }
            catch (Exception e)
            {
                logger.LogError("[SyncCnStock] UpsetCnLevel1ArchivedDaily stock id : {}, Exception : {}",
                    cnStockItem.StockId, e);
            }
        }
    }

    public override Task Execute(IJobExecutionContext context)
    {
        return SyncCnStock();
    }
}
