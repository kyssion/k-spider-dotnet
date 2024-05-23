namespace k_spider_dotnet.script.df_stoke;

public interface IStockSpider
{
    public Task<string> Level1Listening(string stockId);
    public Task<string> GetLevel1DailyArchived(string stockId);
}