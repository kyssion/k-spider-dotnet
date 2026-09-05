using k_spider_dotnet.tool.http;

namespace k_spider_dotnet.spider.df_stock;

public interface IStockSpider
{
    public Task<string> Level1Listening(string stockId);
    public Task<string> GetLevel1DailyArchived(string stockId);
}

public abstract class AbsStockSpider : IStockSpider
{
    public async Task<string> GetLevel1DailyArchived(string url , string channelId ,string stockId)
    {
        var listeningUrl = string.Format(url, channelId, stockId);
        using var client = HttpClientTools.GetHttpClient();
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
        if (result == null || result.EndsWith("\"data\":null}"))
        {
             return "";
        }
        return result;
    }
    
    public abstract Task<string> Level1Listening(string stockId);
    public abstract Task<string> GetLevel1DailyArchived(string stockId);
}