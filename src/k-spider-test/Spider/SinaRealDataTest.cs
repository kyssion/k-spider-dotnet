using KSpider.Exceptions;
using KSpider.Spider;
using KSpider.Spider.News;
using KSpider.Spider.SinaNews;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     新浪财经 7x24 : 真实抓取数据的解析回归 ( 夹具为 2026-09-19 接口原样响应 , 测试离线 )
/// </summary>
[TestClass]
public class SinaRealDataTest
{
    private const int RequestSize = 20;

    private static string LoadFixture(string fileName)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
    }

    [TestMethod]
    public void ParseRealPageMapEveryItemListRow()
    {
        var listPage = SinaNewsSpider.ParseListPage(LoadFixture("sina_live_page1.json"), RequestSize, 1);

        Assert.AreEqual(RequestSize, listPage.Items.Count);
        Assert.AreEqual(listPage.Items.Count, listPage.InlineOrigins.Count);
        Assert.IsTrue(listPage.Items.All(item => item.NewsUrl!.StartsWith("http")));
        Assert.IsTrue(listPage.Items.All(item => !string.IsNullOrEmpty(item.NewsTitle)));
        Assert.IsTrue(listPage.Items.All(item => item.NewsFrom == SinaNewsResource.NewsFromName));
        Assert.IsTrue(listPage.Items.All(item => item.FromMedia == (int)FromTypeOfNews.SinaMedia));
        Assert.IsTrue(listPage.Items.All(item => item.Category == SinaNewsResource.LiveCategoryNumber));
        Assert.IsTrue(listPage.Items.All(item => item.NewsTime?.Year == 2026 && item.NewsTime?.Month == 9));
        // news_url 是去重键 , 真实数据里必须互不相同
        Assert.AreEqual(listPage.Items.Count, listPage.Items.Select(item => item.NewsUrl).Distinct().Count());
    }

    [TestMethod]
    public void ParseRealOriginRoundTripToContent()
    {
        var spider = new SinaNewsSpider();
        var listPage = SinaNewsSpider.ParseListPage(LoadFixture("sina_live_page1.json"), RequestSize, 1);

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
    public void ParseRealPageTwoContinuesOlder()
    {
        var page1 = SinaNewsSpider.ParseListPage(LoadFixture("sina_live_page1.json"), RequestSize, 1);
        var page2 = SinaNewsSpider.ParseListPage(LoadFixture("sina_live_page2.json"), RequestSize, 2);

        Assert.AreEqual("2", page1.NextCursor);
        var oldestOfPage1 = page1.Items.Min(item => item.NewsTime);
        Assert.IsTrue(page2.Items.All(item => item.NewsTime < oldestOfPage1));
        Assert.AreEqual(0, page1.Items.Select(item => item.NewsUrl).Intersect(page2.Items.Select(item => item.NewsUrl)).Count());
    }

    [TestMethod]
    public void ExtractTitleFromBracketHeadline()
    {
        var json = """
                   {"result":{"status":{"code":0},"data":{"feed":{"list":[
                     {"id":1,"rich_text":"【央行开展逆回购操作】央行今日开展1000亿元逆回购操作。","create_time":"2026-09-19 10:00:00","docurl":"https://finance.sina.cn/7x24/2026-09-19/detail-abc.d.html","tag":[{"id":"8","name":"央行"}]}
                   ]}}}}
                   """;

        var listPage = SinaNewsSpider.ParseListPage(json, RequestSize, 1);
        var spider = new SinaNewsSpider();
        var parseResult = spider.ParseContent(listPage.InlineOrigins[0].NewsOriginContent,
            listPage.InlineOrigins[0].NewsUrl);

        Assert.AreEqual("央行开展逆回购操作", listPage.Items[0].NewsTitle);
        Assert.AreEqual("央行", parseResult.Content.NewsKeyword);
    }

    [TestMethod]
    public void FallbackTitleAndUrlWhenMissing()
    {
        var longText = new string('讯', 100);
        // 手拼而非内插原始字符串 : 结尾的 ]}}}} 会与 $$""" 的转义规则冲突
        var json = "{\"result\":{\"status\":{\"code\":0},\"data\":{\"feed\":{\"list\":[" +
                   $"{{\"id\":2,\"rich_text\":\"{longText}\",\"create_time\":\"2026-09-19 10:00:00\",\"docurl\":\"\"}}" +
                   "]}}}}";

        var listPage = SinaNewsSpider.ParseListPage(json, RequestSize, 1);

        // 无【】标题时用正文前 60 字兜底 , docurl 缺失时用合成去重键
        Assert.AreEqual(60, listPage.Items[0].NewsTitle!.Length);
        Assert.AreEqual("https://finance.sina.com.cn/7x24/#feed-2", listPage.Items[0].NewsUrl);
    }

    [TestMethod]
    public void ParseListPageThrowOnErrorStatus()
    {
        Assert.ThrowsExactly<HtmlFormException>(() =>
            SinaNewsSpider.ParseListPage("""{"result":{"status":{"code":1,"msg":"err"}}}""", RequestSize, 1));
    }
}
