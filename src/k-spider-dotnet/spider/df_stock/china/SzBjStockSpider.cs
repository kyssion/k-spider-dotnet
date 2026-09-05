namespace k_spider_dotnet.spider.df_stock.china;

public class SzBjStockSpider : ChinaStockSpider
{
    protected override string GetExchangeChannel()
    {
        return ((int)StockExchangeChannel.SzBjStockExchangeChannel).ToString();
    }
}