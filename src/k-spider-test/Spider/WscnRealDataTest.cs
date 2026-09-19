using KSpider.Exceptions;
using KSpider.Spider;
using KSpider.Spider.News;
using KSpider.Spider.WscnNews;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     华尔街见闻 live : 真实抓取数据的解析回归 ( 夹具为 2026-09-19 接口原样响应 , 测试离线 )
/// </summary>
[TestClass]
public class WscnRealDataTest
{
    private static string LoadFixture(string fileName)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
    }

    [TestMethod]
    public void ParseRealPageMapEveryItemListRow()
    {
        var listPage = WscnNewsSpider.ParseListPage(LoadFixture("wscn_live_page1.json"));

        Assert.AreEqual(20, listPage.Items.Count);
        Assert.AreEqual(listPage.Items.Count, listPage.InlineOrigins.Count);
        Assert.IsTrue(listPage.Items.All(item => item.NewsUrl!.StartsWith("https://wallstreetcn.com/livenews/")));
        Assert.IsTrue(listPage.Items.All(item => !string.IsNullOrEmpty(item.NewsTitle)));
        Assert.IsTrue(listPage.Items.All(item => item.NewsFrom == WscnNewsResource.NewsFromName));
        Assert.IsTrue(listPage.Items.All(item => item.FromMedia == (int)FromTypeOfNews.WscnMedia));
        Assert.IsTrue(listPage.Items.All(item => item.Category == WscnNewsResource.LiveCategoryNumber));
        Assert.IsTrue(listPage.Items.All(item => item.NewsTime?.Year == 2026 && item.NewsTime?.Month == 9));
        Assert.AreEqual(listPage.Items.Count, listPage.Items.Select(item => item.NewsUrl).Distinct().Count());
        Assert.IsNotNull(listPage.NextCursor);
    }

    [TestMethod]
    public void ParseRealOriginRoundTripToContent()
    {
        var spider = new WscnNewsSpider();
        var listPage = WscnNewsSpider.ParseListPage(LoadFixture("wscn_live_page1.json"));

        foreach (var origin in listPage.InlineOrigins)
        {
            var parseResult = spider.ParseContent(origin.NewsOriginContent, origin.NewsUrl);

            Assert.AreEqual(origin.NewsUrl, parseResult.Content.NewsUrl);
            Assert.IsFalse(string.IsNullOrWhiteSpace(parseResult.Content.NewsTitle));
            Assert.IsFalse(string.IsNullOrWhiteSpace(parseResult.Content.NewsContentText));
            Assert.IsNotNull(parseResult.Content.NewsTime);
            Assert.IsTrue(parseResult.Content.NewsContentJson!.Contains(NewsContentSegment.TextType));
        }
    }

    [TestMethod]
    public void ParseRealSecondPageContinuesOlder()
    {
        var page1 = WscnNewsSpider.ParseListPage(LoadFixture("wscn_live_page1.json"));
        var page2 = WscnNewsSpider.ParseListPage(LoadFixture("wscn_live_page2.json"));

        // 游标是接口给的 next_cursor , 不含边界条目
        var oldestOfPage1 = page1.Items.Min(item => item.NewsTime);
        Assert.IsTrue(page2.Items.All(item => item.NewsTime < oldestOfPage1));
        Assert.AreEqual(0, page1.Items.Select(item => item.NewsUrl).Intersect(page2.Items.Select(item => item.NewsUrl)).Count());
    }

    [TestMethod]
    public void FallbackTitleToContentWhenTitleMissing()
    {
        var json = """
                   {"code":20000,"message":"OK","data":{"next_cursor":1789785930,"items":[
                     {"id":1,"title":"","content_text":"见闻快讯正文","display_time":1789785730,"uri":"https://wallstreetcn.com/livenews/1","images":[],"tags":[]}
                   ]}}
                   """;

        var listPage = WscnNewsSpider.ParseListPage(json);

        Assert.AreEqual("见闻快讯正文", listPage.Items[0].NewsTitle);
        Assert.AreEqual("1789785930", listPage.NextCursor);
    }

    [TestMethod]
    public void ParseListPageThrowOnErrorCode()
    {
        Assert.ThrowsExactly<HtmlFormException>(() =>
            WscnNewsSpider.ParseListPage("""{"code":40001,"message":"invalid"}"""));
    }
}
