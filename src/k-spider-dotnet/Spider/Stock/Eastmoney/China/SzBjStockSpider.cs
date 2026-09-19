namespace KSpider.Spider.Stock.Eastmoney.China;

public class SzBjStockSpider : ChinaStockSpider
{
    protected override string GetExchangeChannel()
    {
        return ((int)StockExchangeChannel.SzBjStockExchangeChannel).ToString();
    }
}