namespace k_spider_dotnet.spider.df_stock.china;

public class ShStockSpider : ChinaStockSpider
{
    protected override string GetExchangeChannel()
    {
        return ((int)StockExchangeChannel.ShangHStockExchangeChannel).ToString();
    }
}