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
        new StockCnJob().SyncCnStock();
    }
    [TestMethod]
    public void FindHkStokeInfo()
    {
        new StockHkJob().SyncCnStock();
    }
}