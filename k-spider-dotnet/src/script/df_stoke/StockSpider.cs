namespace k_spider_dotnet.script.df_stoke;

public interface IStockSpider
{
    public Task<string> Listening(string stockId);
    public Task<string> DownloadArchived(string stockId);
}