using k_spider_dotnet_lib.logger;
using Microsoft.Extensions.Logging;

namespace k_spider_dotnet.script.df_stoke.hk;

public class HkStockSpider : AbsStockSpider
{
    private const string Url =
        "https://17.push2.eastmoney.com/api/qt/stock/details/sse?fields1=f1,f2,f3,f4&fields2=f51,f52,f53,f54,f55&mpi=1000&fltt=2&pos=0&secid={0}.{1}&wbp2u=|0|0|0|web";

    private static readonly ILogger Logger = LogFactory.GetLogger<HkStockSpider>();

    public override Task<string> Level1Listening(string stockId)
    {
        throw new NotImplementedException();
    }

    public override async Task<string> GetLevel1DailyArchived(string stockId)
    {
        try
        {
            var ans = await base.GetLevel1DailyArchived(Url, "116", stockId);
            if (ans == "")
            {
                Logger.LogError("[HkStockSpider] result failed , stock id : {}", stockId);
            }
            return ans;
        }
        catch (Exception e)
        {
            Logger.LogError("[HkStockSpider] result Exception , stock id : {} , exception : {}", stockId,e);
            return "";
        }
    }
}