using System.Text.Json;
using k_spider_dotnet.dal.db;
using k_spider_dotnet.dao;
using k_spider_dotnet.script.df_news.spider;
using k_spider_dotnet.tool.log;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace k_spider_dotnet_test.spider_test.df_news;

[TestClass]
public class DfContentSpiderTest
{
    private readonly ILogger _logger = LogFactory.GetLogger<DfContentSpiderTest>();

    [TestMethod]
    public void TestDfNewsContextSpider()
    {
        var dfContextInfo =
            new DfContentSpider().GetDfContextInfoByUrl("http://biz.eastmoney.com/news/1670,202203042297564271.html")
                .Result;
        Console.WriteLine(JsonSerializer.Serialize(dfContextInfo));
        dfContextInfo =
            new DfContentSpider().GetDfContextInfoByUrl("http://fund.eastmoney.com/news/1593,202402222992098190.html")
                .Result;
        Console.WriteLine(JsonSerializer.Serialize(dfContextInfo));
    }

    // 测试东方财富历史的代码为信息
    [TestMethod]
    public void TestDfOldContextSpider()
    {
        var dfContentInfo =
            new DfContentSpider()
                .GetDfContextInfoByUrlInterface("https://finance.eastmoney.com/a/202404283062764568.html").Result;
        var connection = Pg.Connection();
        _logger.LogInformation("{}", SpiderNewsListDao.UpsetSpiderNewsContent(connection, dfContentInfo.ToSpiderNewsContentTestModel()));
        // _logger.LogInformation("{}",connection.Storageable(itemList).WhereColumns(it => it.NewsUrl).ExecuteCommand());
        _logger.LogInformation("{0}", DateTime.Now);
        
        Console.WriteLine(JsonSerializer.Serialize(dfContentInfo));
    }
}