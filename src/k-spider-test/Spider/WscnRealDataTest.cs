using KSpider.Exceptions;
using KSpider.Spider;
using KSpider.Spider.News.Flash.Wscn;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     华尔街见闻 live : 真实抓取数据的快讯解析回归 ( 夹具为 2026-09-19 接口原样响应 , 测试离线 )
/// </summary>
[TestClass]
public class WscnRealDataTest
{
    private static string LoadFixture(string fileName)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
    }

    [TestMethod]
    public void ParseRealPageMapEveryFlashRecord()
    {
        var page = WscnNewsSpider.ParseFlashPage(LoadFixture("wscn_live_page1.json"));

        Assert.AreEqual(20, page.Items.Count);
        Assert.IsTrue(page.Items.All(item => item.NewsUrl.StartsWith("https://wallstreetcn.com/livenews/")));
        Assert.IsTrue(page.Items.All(item => !string.IsNullOrEmpty(item.Title)));
        Assert.IsTrue(page.Items.All(item => !string.IsNullOrEmpty(item.Content)));
        Assert.IsTrue(page.Items.All(item => item.FromMedia == (int)FromTypeOfNews.WscnMedia));
        Assert.IsTrue(page.Items.All(item => item.Category == WscnNewsResource.LiveCategoryNumber));
        Assert.IsTrue(page.Items.All(item => item.NewsTime.Year == 2026 && item.NewsTime.Month == 9));
        Assert.AreEqual(page.Items.Count, page.Items.Select(item => item.NewsUrl).Distinct().Count());
        Assert.IsNotNull(page.NextCursor);
    }

    [TestMethod]
    public void ParseRealLevelFromScore()
    {
        var page = WscnNewsSpider.ParseFlashPage(LoadFixture("wscn_live_page1.json"));

        // 真实夹具 20 条 : score=1 x18 ( level 1 ) + score=2 x2 ( level 2 重要 )
        Assert.AreEqual(18, page.Items.Count(item => item.Level == 1));
        Assert.AreEqual(2, page.Items.Count(item => item.Level == 2));
    }

    [TestMethod]
    public void ParseRealSecondPageContinuesOlder()
    {
        var page1 = WscnNewsSpider.ParseFlashPage(LoadFixture("wscn_live_page1.json"));
        var page2 = WscnNewsSpider.ParseFlashPage(LoadFixture("wscn_live_page2.json"));

        // 游标是接口给的 next_cursor , 不含边界条目
        var oldestOfPage1 = page1.Items.Min(item => item.NewsTime);
        Assert.IsTrue(page2.Items.All(item => item.NewsTime < oldestOfPage1));
        Assert.AreEqual(0,
            page1.Items.Select(item => item.NewsUrl).Intersect(page2.Items.Select(item => item.NewsUrl)).Count());
    }

    [TestMethod]
    public void FallbackTitleToContentWhenTitleMissing()
    {
        var json = """
                   {"code":20000,"message":"OK","data":{"next_cursor":1789785930,"items":[
                     {"id":1,"title":"","content_text":"见闻快讯正文","display_time":1789785730,"uri":"https://wallstreetcn.com/livenews/1","images":[],"tags":[]}
                   ]}}
                   """;

        var page = WscnNewsSpider.ParseFlashPage(json);

        Assert.AreEqual("见闻快讯正文", page.Items[0].Title);
        Assert.AreEqual("1789785930", page.NextCursor);
    }

    [TestMethod]
    public void ParseFlashPageThrowOnErrorCode()
    {
        Assert.ThrowsExactly<HtmlFormException>(() =>
            WscnNewsSpider.ParseFlashPage("""{"code":40001,"message":"invalid"}"""));
    }
}
