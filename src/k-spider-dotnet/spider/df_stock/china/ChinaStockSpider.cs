using k_spider_dotnet.logger;
using Microsoft.Extensions.Logging;

namespace k_spider_dotnet.spider.df_stock.china;

public abstract class ChinaStockSpider : AbsStockSpider
{
    private const string Url =
        "https://13.push2.eastmoney.com/api/qt/stock/details/sse?fields1=f1,f2,f3,f4&fields2=f51,f52,f53,f54,f55&mpi=2000&ut=bd1d9ddb04089700cf9c27f6f7426281&fltt=2&pos=-0&secid={0}.{1}&wbp2u=|0|0|0|web";

    private static readonly ILogger Logger = LogFactory.GetLogger<ChinaStockSpider>();

    public override Task<string> Level1Listening(string stockId)
    {
        throw new NotImplementedException();
    }

    public override async Task<string> GetLevel1DailyArchived(string stockId)
    {
        try
        {
            var ans = await base.GetLevel1DailyArchived(Url, GetExchangeChannel(), stockId);
            if (ans == "")
            {
                Logger.LogError("[ChinaStockSpider] result failed , stock id : {}", stockId);
            }
            return ans;
        }
        catch (Exception e)
        {
            Logger.LogError("[ChinaStockSpider] result Exception , stock id : {} , exception : {}", stockId,e);
            return "";
        }
    }

    protected abstract string GetExchangeChannel();
}