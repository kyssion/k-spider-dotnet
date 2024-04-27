using k_spider_dotnet.script.df_news.spider;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace k_spider_dotnet_test.spider_test.df_news;

[TestClass]
public class DfContextSpiderTest
{
   [TestMethod]
   public void TestDfNewsContextSpider()
   {
      var dfContextInfo =
         new DfContextSpider().GetDfContextInfoByUrl("https://finance.eastmoney.com/a/202403193016926098.html").Result;
   }
   
   // 测试东方财富历史的代码为信息
   [TestMethod]
   public void TestDfOldContextSpider()
   {
      var dfContextInfo =
         new DfContextSpider().GetDfContextInfoByUrl("https://fund.eastmoney.com/a/1593,202108032028758863.html").Result;
   }
}