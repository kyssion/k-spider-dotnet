using k_spider_dotnet.dal.db;
using k_spider_dotnet.job.dfStockJob;
using k_spider_dotnet.model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace k_spider_dotnet_test.spider_test.df_stock;

[TestClass]
public class StokeTest
{
    [TestMethod]
    public void FindCnStokeInfo()
    {
        var connection = Pg.Connection();
        var  items = connection.Queryable<StockCnLevel1ArchivedDailyOriginModel>().ToList();
        new StockCnJob().SyncCnStock();
    }
}