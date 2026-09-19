using KSpider.Exceptions;
using KSpider.Spider;
using KSpider.Spider.SinaNews;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     新浪财经 7x24 : 真实抓取数据的快讯解析回归 ( 夹具为 2026-09-19 接口原样响应 , 测试离线 )
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
    public void ParseRealPageMapEveryFlashRecord()
    {
        var page = SinaNewsSpider.ParseFlashPage(LoadFixture("sina_live_page1.json"), RequestSize, 1);

        Assert.AreEqual(RequestSize, page.Items.Count);
        Assert.IsTrue(page.Items.All(item => item.NewsUrl.StartsWith("http")));
        Assert.IsTrue(page.Items.All(item => !string.IsNullOrEmpty(item.Title)));
        Assert.IsTrue(page.Items.All(item => !string.IsNullOrEmpty(item.Content)));
        Assert.IsTrue(page.Items.All(item => item.FromMedia == (int)FromTypeOfNews.SinaMedia));
        Assert.IsTrue(page.Items.All(item => item.Category == SinaNewsResource.LiveCategoryNumber));
        Assert.IsTrue(page.Items.All(item => item.NewsTime.Year == 2026 && item.NewsTime.Month == 9));
        // 新浪无重要度字段 , 恒 1
        Assert.IsTrue(page.Items.All(item => item.Level == 1));
        Assert.AreEqual(page.Items.Count, page.Items.Select(item => item.NewsUrl).Distinct().Count());
        Assert.AreEqual("2", page.NextCursor);
    }

    [TestMethod]
    public void ParseRealStockListFromExt()
    {
        var page = SinaNewsSpider.ParseFlashPage(LoadFixture("sina_live_page1.json"), RequestSize, 1);

        // 真实夹具 20 条里 12 条带关联标的 ( ext 字段内嵌 stocks 数组 )
        var withStocks = page.Items.Where(item => !string.IsNullOrEmpty(item.StockList)).ToList();
        Assert.IsTrue(withStocks.Count > 0, "真实数据应存在带关联标的的条目");
        var first = withStocks[0].StockList!;
        Assert.IsTrue(first.StartsWith("[{\"stock_id\":\""));
        Assert.IsTrue(first.Contains("\"name\":\""));
    }

    [TestMethod]
    public void ParseRealSecondPageContinuesOlder()
    {
        var page1 = SinaNewsSpider.ParseFlashPage(LoadFixture("sina_live_page1.json"), RequestSize, 1);
        var page2 = SinaNewsSpider.ParseFlashPage(LoadFixture("sina_live_page2.json"), RequestSize, 2);

        var oldestOfPage1 = page1.Items.Min(item => item.NewsTime);
        Assert.IsTrue(page2.Items.All(item => item.NewsTime < oldestOfPage1));
        Assert.AreEqual(0,
            page1.Items.Select(item => item.NewsUrl).Intersect(page2.Items.Select(item => item.NewsUrl)).Count());
    }

    [TestMethod]
    public void FallbackTitleAndUrlWhenMissing()
    {
        var longText = new string('讯', 100);
        // 手拼而非内插原始字符串 : 结尾的 ]}}}} 会与 $$""" 的转义规则冲突
        var json = "{\"result\":{\"status\":{\"code\":0},\"data\":{\"feed\":{\"list\":[" +
                   $"{{\"id\":2,\"rich_text\":\"{longText}\",\"create_time\":\"2026-09-19 10:00:00\",\"docurl\":\"\"}}" +
                   "]}}}}";

        var page = SinaNewsSpider.ParseFlashPage(json, RequestSize, 1);

        // 无【】标题时用正文前 60 字兜底 , docurl 缺失时用合成去重键
        Assert.AreEqual(60, page.Items[0].Title!.Length);
        Assert.AreEqual("https://finance.sina.com.cn/7x24/#feed-2", page.Items[0].NewsUrl);
    }

    [TestMethod]
    public void ParseFlashPageThrowOnErrorStatus()
    {
        Assert.ThrowsExactly<HtmlFormException>(() =>
            SinaNewsSpider.ParseFlashPage("""{"result":{"status":{"code":1,"msg":"err"}}}""", RequestSize, 1));
    }
}
