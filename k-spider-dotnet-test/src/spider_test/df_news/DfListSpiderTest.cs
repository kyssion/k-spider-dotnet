using k_spider_dotnet.dal.db;
using k_spider_dotnet.dao;
using k_spider_dotnet.model;
using k_spider_dotnet.script.df_news.spider;
using k_spider_dotnet.script.df_news.spider.playwright;
using k_spider_dotnet.tool.log;
using k_spider_dotnet.tool.resource;
using k_spider_dotnet.tool.time;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace k_spider_dotnet_test.spider_test.df_news;

[TestClass]
public class DfListSpiderTest
{
    private readonly ILogger _logger = LogFactory.GetLogger<DfListSpiderTest>();

    [TestMethod]
    public void TestDfListWithPlaywright()
    {
        var number = new DfListSpiderWithPlaywright(false, true).GetListResourceNumberInfo(
            "https://finance.eastmoney.com/a/ccjdd_1.html").Result;
    }

    [TestMethod]
    public void TestGetDfList()
    {
        var startTime = DateTime.Now;
        var dfListItem = new DfListSpiderWithPlaywright(false, true);
        var list = dfListItem.GetDfListInfo("https://finance.eastmoney.com/a/ccjdd_{0}.html", 10)
            .Result;
    }

    [TestMethod]
    public void TestGetDfListByUrl()
    {
        var needUrlInfo = DfResource.DfListUrlResourceList[5];
        var dfListItem = new DfListSpider()
            .GetDfListInfoByUrl(needUrlInfo, 25, 25, 200, DfListOrderType.ByTime).Result;
        var connection = Pg.Connection();
        var itemList = dfListItem.Select(item => new SpiderNewsListModel
            {
                FromMedia = (int)NewsFromType.DfMedia,
                NewsUrl = item.NewsUrl,
                NewsTitle = item.NewsTitle,
                NewsSummary = item.NewsSummary,
                NewsFrom = item.NewsFrom,
                NewsTime = TimeTools.GetDateByTimeStrForFormat(item.NewsTime ?? "", TimeTools.TimeFormatForStrikethrough),
                NewsDownloadTime = item.NewsDownloadTime,
                Category = item.Category
            }).GroupBy(item => item.NewsUrl).Select(item => item.First())
            .ToList();

        _logger.LogInformation("{}", SpiderNewsListDao.UpsertSpiderNewsList(connection, itemList, 200));
        // _logger.LogInformation("{}",connection.Storageable(itemList).WhereColumns(it => it.NewsUrl).ExecuteCommand());
        _logger.LogInformation("{0}", DateTime.Now);
    }
}