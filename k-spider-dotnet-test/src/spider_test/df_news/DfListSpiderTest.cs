using System.Text.Encodings.Web;
using System.Text.Json;
using k_spider_dotnet_test.spider_test.tool;
using k_spider_dotnet.dal.db;
using k_spider_dotnet.model;
using k_spider_dotnet.script.df_news.spider;
using k_spider_dotnet.tool.resource;
using k_spider_dotnet.tool.time;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources;

namespace k_spider_dotnet_test.spider_test.df_news;

[TestClass]
public class DfListSpiderTest
{

    [TestMethod]
    public void TestDfListWithPlaywright()
    {
        int number = new DfListSpiderWithPlaywright(false,true).GetListResourceNumberInfo(
            "https://finance.eastmoney.com/a/ccjdd_1.html").Result;
    }
    
    [TestMethod]
    public void TestGetDfList()
    {
        var startTime = DateTime.Now;
        var dfListItem = new DfListSpiderWithPlaywright(false,true);
        var list = dfListItem.GetDfListInfo("https://finance.eastmoney.com/a/ccjdd_{0}.html", 10)
            .Result;
        Console.WriteLine(DateTime.Now.Subtract(startTime).TotalMilliseconds);
    }

    [TestMethod]
    public void TestGetDfListByUrl()
    {
        var needUrlInfo = DfResource.DfListUrlResourceList[0];
        var dfListItem = new DfListSpider()
            .GetDfListInfoByUrl(needUrlInfo.ListResourceNumber, 1, 1, 10, DfListOrderType.ByTime).Result;
        var connection = Pg.Connection();
        var itemList = dfListItem.Select(item => new SpiderNewsListTestModel
            {
                FromMedia = (int)NewsFromType.DfMedia,
                NewsUrl = item.NewsUrl,
                NewsTitle = item.NewsTitle,
                NewsSummary = item.NewsSummary,
                NewsFrom = item.NewsFrom,
                NewsTime = TimeTools.GetDateByTimeStr(item.NewsTime ?? "", TimeTools.DfTimeFormat),
                NewsDownloadTime = item.NewsDownloadTime,
            })
            .ToList();
        Console.WriteLine(connection.Storageable(itemList).WhereColumns(it => it.NewsUrl).ExecuteCommand());
    }
}