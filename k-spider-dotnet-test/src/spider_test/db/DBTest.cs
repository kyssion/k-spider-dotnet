using k_spider_dotnet_test.spider_test.tool.developer;
using k_spider_dotnet.dal.db;
using k_spider_dotnet.model;
using k_spider_dotnet.tool.resource;
using Microsoft.Playwright;

namespace k_spider_dotnet_test.spider_test.db;

[TestClass]
public class DbTest
{
    [TestMethod]
    public void TestCreatePgModer()
    {
       BuildPg.CreatePgModer();
    }

    [TestMethod]
    public void UpsetNewsList()
    {
        var connection = Pg.Connection();
        var newListItem = new SpiderNewsListTestModel
        {
            FromMedia = (int)NewsFromType.DfMedia,
            NewsUrl = "NewsUrl2",
            NewsTitle = "NewsTasvbasdfaseaeitle2",
            NewsSummary = "NfawefwefewsSummasdfary3",
            NewsFrom = "Nfsdfsfsdfs4",
            NewsTime = DateTime.Now,
            NewsDownloadTime = DateTime.Now,
        };
        Console.WriteLine(connection.Storageable(new List<SpiderNewsListTestModel>(){newListItem}).WhereColumns(it=>it.NewsUrl).
            ToStorage().AsInsertable.ExecuteCommand());
    }

    [TestMethod]
    public void InsertInfoNewsList()
    {
        var connection = Pg.Connection();
        var newListItem = new SpiderNewsListTestModel
        {
            FromMedia = (int)NewsFromType.DfMedia,
            NewsUrl = "NewsUrl",
            NewsTitle = "NewsTitle",
            NewsSummary = "NewsSummary",
            NewsFrom = "NewsFrom",
            NewsTime = DateTime.Now,
            NewsDownloadTime = DateTime.Now,
        };
        Console.WriteLine(connection.Insertable(newListItem).ExecuteCommand());
    }
}