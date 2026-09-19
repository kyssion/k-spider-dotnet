using KSpider.Spider;
using KSpider.Spider.ClsNews;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     财联社电报 : 真实抓取数据的解析回归 ( 夹具为 2026-09-19 接口原样响应 , 测试离线 )
/// </summary>
[TestClass]
public class ClsRealDataTest
{
    private const string Page1File = "cls_roll_page1.json";
    private const string Page2File = "cls_roll_page2.json";
    private const string ImageItemFile = "cls_roll_with_image.json";

    private const int RequestSize = 20;

    private static string LoadFixture(string fileName)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
    }

    [TestMethod]
    public void ParseRealPageMapEveryItemListRow()
    {
        var listPage = ClsNewsSpider.ParseListPage(LoadFixture(Page1File), RequestSize);

        Assert.AreEqual(RequestSize, listPage.Items.Count);
        Assert.AreEqual(listPage.Items.Count, listPage.InlineOrigins.Count);
        Assert.IsTrue(listPage.Items.All(item => item.NewsUrl!.StartsWith("https://www.cls.cn/detail/")));
        Assert.IsTrue(listPage.Items.All(item => !string.IsNullOrEmpty(item.NewsTitle)));
        Assert.IsTrue(listPage.Items.All(item => !string.IsNullOrEmpty(item.NewsFrom) && item.NewsFrom == "财联社"));
        Assert.IsTrue(listPage.Items.All(item => item.FromMedia == (int)FromTypeOfNews.ClsMedia));
        Assert.IsTrue(listPage.Items.All(item => item.Category == ClsNewsResource.TelegraphCategoryNumber));
        Assert.IsTrue(listPage.Items.All(item => item.NewsTime?.Year == 2026 && item.NewsTime?.Month == 9));
        // news_url 是去重键 , 真实数据里必须互不相同
        Assert.AreEqual(listPage.Items.Count, listPage.Items.Select(item => item.NewsUrl).Distinct().Count());
        // 真实数据里两种形态并存 : 部分取 title 字段 , 部分用 brief 兜底
        Assert.IsTrue(listPage.Items.Any(item => item.NewsTitle != item.NewsSummary), "应有取 title 字段的条目");
        Assert.IsTrue(listPage.Items.Any(item => item.NewsTitle == item.NewsSummary), "应有 brief 兜底的条目");
    }

    [TestMethod]
    public void ParseRealOriginRoundTripToContent()
    {
        var spider = new ClsNewsSpider();
        var parsedCount = 0;
        foreach (var fileName in new[] { Page1File, Page2File })
        {
            var listPage = ClsNewsSpider.ParseListPage(LoadFixture(fileName), RequestSize);
            foreach (var origin in listPage.InlineOrigins)
            {
                var parseResult = spider.ParseContent(origin.NewsOriginContent, origin.NewsUrl);

                Assert.AreEqual(origin.NewsUrl, parseResult.Content.NewsUrl);
                Assert.IsFalse(string.IsNullOrWhiteSpace(parseResult.Content.NewsTitle));
                Assert.IsFalse(string.IsNullOrWhiteSpace(parseResult.Content.NewsContentText));
                Assert.IsFalse(string.IsNullOrWhiteSpace(parseResult.Content.NewsKeyword));
                Assert.IsNotNull(parseResult.Content.NewsTime);
                Assert.IsTrue(parseResult.Content.NewsContentJson!.Contains("\"TEXT\""));
                parsedCount++;
            }
        }

        Assert.AreEqual(RequestSize * 2, parsedCount);
    }

    [TestMethod]
    public void ParseRealCursorChainContinuesOlder()
    {
        var page1 = ClsNewsSpider.ParseListPage(LoadFixture(Page1File), RequestSize);
        var page2 = ClsNewsSpider.ParseListPage(LoadFixture(Page2File), RequestSize);

        // 第二页 ( 用第一页游标抓取 ) 的条目全部不晚于第一页最老一条
        var oldestOfPage1 = page1.Items.Min(item => item.NewsTime);
        Assert.IsTrue(page2.Items.All(item => item.NewsTime <= oldestOfPage1));
        // 游标 +1 的代价 : 边界那一条会被第二页重复取回 , 去重交给入库时的 ON CONFLICT
        var overlap = page1.Items.Select(item => item.NewsUrl).Intersect(page2.Items.Select(item => item.NewsUrl));
        Assert.AreEqual(1, overlap.Count());
        // 游标 = 本页最老一条 ctime(1789746671) + 1 , 两页都是满页所以都给出下一页游标
        Assert.AreEqual("1789746672", page1.NextCursor);
        Assert.IsNotNull(page2.NextCursor);
    }

    [TestMethod]
    public void ParseRealItemWithImageExtractsImage()
    {
        var spider = new ClsNewsSpider();
        var listPage = ClsNewsSpider.ParseListPage(LoadFixture(ImageItemFile), RequestSize);

        var parseResult = spider.ParseContent(listPage.InlineOrigins[0].NewsOriginContent,
            listPage.InlineOrigins[0].NewsUrl);

        Assert.AreEqual(1, parseResult.Images.Count);
        Assert.AreEqual("https://image.cls.cn/images/20260918/6ws9O4Q9UJ_798x1096.png",
            parseResult.Images[0].ImageResourceUrl);
        Assert.AreEqual("6ws9O4Q9UJ_798x1096.png", parseResult.Images[0].ImageName);
        Assert.IsTrue(parseResult.Content.NewsContentJson!.Contains("\"IMG\""));
    }
}
