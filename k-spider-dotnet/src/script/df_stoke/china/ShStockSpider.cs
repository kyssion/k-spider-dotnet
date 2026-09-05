namespace k_spider_dotnet.script.df_stoke.china;

public class ShStockSpider : ChinaStockSpider
{
    protected override string GetExchangeChannel()
    {
        return ((int)StockExchangeChannel.ShangHStockExchangeChannel).ToString();
    }
}