using k_spider_dotnet.dal.db;
using k_spider_dotnet.dao;
using k_spider_dotnet.script.df_news.spider;
using k_spider_dotnet.tool.log;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace k_spider_dotnet_test.spider_test.df_news;

[TestClass]
public class DfListSpiderTest
{
    private readonly ILogger Logger = LogFactory.GetLogger<DfListSpiderTest>();

    [TestMethod]
    public void TestNewsResourceForDfcf()
    {
        const int startNumber = 1;
        const int endNumber = 30;
        const int pageSize = 200;
        var allNumber = 0;
        const DfListOrderType orderType = DfListOrderType.ByTime;
        using var connection = Pg.Connection();
        foreach (var resourceItem in DfResource.DfListUrlResourceList[20..])
            try
            {
                var dfListInfos = new DfListSpider()
                    .GetDfListInfoByUrl(resourceItem, startNumber, endNumber, pageSize, orderType).Result;
                var dbDfListInfos = dfListInfos.Select(item => item.ToSpiderNewListModel())
                    .ToList();
                allNumber += SpiderNewsDao.UpsetSpiderNewsListInfo(connection, dbDfListInfos);
            }
            catch (Exception e)
            {
                Logger.LogError("[DfNewsJob Execute]  run error : {}", e);
            }

        Logger.LogInformation("[DfNewsJob Execute] success news url number : {}", allNumber);
    }
}