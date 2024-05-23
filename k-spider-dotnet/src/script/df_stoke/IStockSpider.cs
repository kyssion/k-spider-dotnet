namespace k_spider_dotnet.script.df_stoke;

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
        if (result == null || result.EndsWith("\"data\":null}"))
        {
            return "";
        }
        return result;
    }
    
    public abstract Task<string> Level1Listening(string stockId);
    public abstract Task<string> GetLevel1DailyArchived(string stockId);
}