namespace KSpider.Spider.DfStock.China;

public class ShStockSpider : ChinaStockSpider
{
    protected override string GetExchangeChannel()
    {
        return ((int)StockExchangeChannel.ShangHStockExchangeChannel).ToString();
    }
}