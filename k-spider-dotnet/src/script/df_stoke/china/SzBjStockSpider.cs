namespace k_spider_dotnet.script.df_stoke.china;

public class SzBjStockSpider : ChinaStockSpider
{
    protected override string GetExchangeChannel()
    {
        return ((int)StockExchangeChannel.SzBjStockExchangeChannel).ToString();
    }
}