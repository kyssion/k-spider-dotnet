using k_spider_dotnet.tool.log;
using Microsoft.Extensions.Logging;

namespace k_spider_dotnet.script.df_stoke.china;

public abstract class ChinaStockSpider : IStockSpider
{
    private const string Url =
        "https://13.push2.eastmoney.com/api/qt/stock/details/sse?fields1=f1,f2,f3,f4&fields2=f51,f52,f53,f54,f55" +
        "&mpi=2000&ut=bd1d9ddb04089700cf9c27f6f7426281&fltt=2&pos=-0&secid={0}.{1}&wbp2u=|0|0|0|web";

    private static readonly ILogger Logger = LogFactory.GetLogger<ChinaStockSpider>();

    public Task<string> Level1Listening(string stockId)
    {
        throw new NotImplementedException();
    }

    public async Task<string> GetLevel1DailyArchived(string stockId)
    {
        var listeningUrl = string.Format(Url, GetExchangeChannel(), stockId);
        using var client = new HttpClient();
        var response = client.GetStreamAsync(listeningUrl).Result;
        var result = "";
        using (var reader = new StreamReader(response))
        {
            while (!reader.EndOfStream)
            {
                result = await reader.ReadLineAsync();
                break;
            }
        }

        if (result == null)
        {
            Logger.LogError("[ShangHStockSpider] Listening : result is null");
            return "";
        }

        if (!result.EndsWith("\"data\":null}")) return result;
        Logger.LogError("[ShangHStockSpider] Listening : data is null");
        return "";
    }

    protected abstract int GetExchangeChannel();
}