using KSpider.Tool.Http;

namespace KSpider.Spider.DfStock;

public interface IStockSpider
{
    public Task<string> Level1Listening(string stockId);
    public Task<string> GetLevel1DailyArchived(string stockId);
}

public abstract class AbsStockSpider : IStockSpider
{
    public async Task<string> GetLevel1DailyArchived(string url, string channelId, string stockId)
    {
        var listeningUrl = string.Format(url, channelId, stockId);
        // 共享 HttpClient 不能 dispose
        var stream = await HttpClientTools.GetHttpClient().GetStreamAsync(listeningUrl);
        using var reader = new StreamReader(stream);
        // SSE 接口只需读归档首帧 , 即第一行
        var result = await reader.ReadLineAsync();
        if (result == null || result.EndsWith("\"data\":null}")) return "";
        return result;
    }

    public abstract Task<string> Level1Listening(string stockId);
    public abstract Task<string> GetLevel1DailyArchived(string stockId);
}
