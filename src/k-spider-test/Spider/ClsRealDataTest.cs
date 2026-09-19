using KSpider.Spider;
using KSpider.Spider.ClsNews;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     财联社电报 : 真实抓取数据的快讯解析回归 ( 夹具为 2026-09-19 接口原样响应 , 测试离线 )
/// </summary>
[TestClass]
public class ClsRealDataTest
{
    private const int RequestSize = 20;

    private static string LoadFixture(string fileName)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
    }

    [TestMethod]
    public void ParseRealPageMapEveryFlashRecord()
    {
        var page = ClsNewsSpider.ParseFlashPage(LoadFixture("cls_roll_page1.json"), RequestSize);

        Assert.AreEqual(RequestSize, page.Items.Count);
        Assert.IsTrue(page.Items.All(item => item.NewsUrl.StartsWith("https://www.cls.cn/detail/")));
        Assert.IsTrue(page.Items.All(item => !string.IsNullOrEmpty(item.Title)));
        Assert.IsTrue(page.Items.All(item => !string.IsNullOrEmpty(item.Content)));
        Assert.IsTrue(page.Items.All(item => !string.IsNullOrEmpty(item.Keyword)));
        Assert.IsTrue(page.Items.All(item => item.FromMedia == (int)FromTypeOfNews.ClsMedia));
        Assert.IsTrue(page.Items.All(item => item.Category == ClsNewsResource.TelegraphCategoryNumber));
        Assert.IsTrue(page.Items.All(item => item.NewsTime.Year == 2026 && item.NewsTime.Month == 9));
        Assert.IsTrue(page.Items.All(item => !string.IsNullOrEmpty(item.RawContent)));
        // 真实夹具 20 条 level 全为 C → 快讯 level 全 1
        Assert.IsTrue(page.Items.All(item => item.Level == 1));
        // news_url 是去重键 , 真实数据里必须互不相同
        Assert.AreEqual(page.Items.Count, page.Items.Select(item => item.NewsUrl).Distinct().Count());
    }

    [TestMethod]
    public void ParseRealCursorChainContinuesOlder()
    {
        var page1 = ClsNewsSpider.ParseFlashPage(LoadFixture("cls_roll_page1.json"), RequestSize);
        var page2 = ClsNewsSpider.ParseFlashPage(LoadFixture("cls_roll_page2.json"), RequestSize);

        // 第二页 ( 用第一页游标抓取 ) 的条目全部不晚于第一页最老一条
        var oldestOfPage1 = page1.Items.Min(item => item.NewsTime);
        Assert.IsTrue(page2.Items.All(item => item.NewsTime <= oldestOfPage1));
        // 游标 +1 的代价 : 边界那一条会被第二页重复取回 , 去重交给入库的 ON CONFLICT
        var overlap = page1.Items.Select(item => item.NewsUrl).Intersect(page2.Items.Select(item => item.NewsUrl));
        Assert.AreEqual(1, overlap.Count());
        // 游标 = 本页最老一条 ctime(1789746671) + 1
        Assert.AreEqual("1789746672", page1.NextCursor);
        Assert.IsNotNull(page2.NextCursor);
    }

    [TestMethod]
    public void ParseRealItemWithImageExtractsImage()
    {
        var page = ClsNewsSpider.ParseFlashPage(LoadFixture("cls_roll_with_image.json"), RequestSize);

        var item = page.Items[0];

        Assert.AreEqual("https://www.cls.cn/detail/2487578", item.NewsUrl);
        Assert.AreEqual("[\"https://image.cls.cn/images/20260918/6ws9O4Q9UJ_798x1096.png\"]", item.ImageUrls);
        Assert.IsNull(item.StockList);
    }
}
