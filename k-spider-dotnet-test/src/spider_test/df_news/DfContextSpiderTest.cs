using k_spider_dotnet.script.df_news.spider;

namespace k_spider_dotnet_test.spider_test.df_news;

[TestClass]
public class DfContextSpiderTest
{
   [TestMethod]
   public void TestDfContextSpider()
   {
      var dfContextInfo =
         new DfContextSpider().GetDfContextInfoByUrl("https://finance.eastmoney.com/a/202403193016926098.html").Result;
   }
}