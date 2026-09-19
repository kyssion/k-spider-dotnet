using KSpider.Exceptions;
using KSpider.Spider;
using KSpider.Spider.Jin10News;
using KSpider.Spider.News;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     金十数据快讯 : 真实抓取数据的解析回归 ( 夹具为 2026-09-19 接口原样响应 , 测试离线 )
/// </summary>
[TestClass]
public class Jin10RealDataTest
{
    private static string LoadFixture(string fileName)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
    }

    [TestMethod]
    public void ParseRealPageMapEveryItemListRow()
    {
        var listPage = Jin10NewsSpider.ParseListPage(LoadFixture("jin10_flash_page1.json"));

        Assert.IsTrue(listPage.Items.Count > 0);
        Assert.AreEqual(listPage.Items.Count, listPage.InlineOrigins.Count);
        Assert.IsTrue(listPage.Items.All(item => item.NewsUrl!.StartsWith("https://flash.jin10.com/detail/")));
        Assert.IsTrue(listPage.Items.All(item => !string.IsNullOrEmpty(item.NewsTitle)));
        Assert.IsTrue(listPage.Items.All(item => item.NewsFrom == Jin10NewsResource.NewsFromName));
        Assert.IsTrue(listPage.Items.All(item => item.FromMedia == (int)FromTypeOfNews.Jin10Media));
        Assert.IsTrue(listPage.Items.All(item => item.Category == Jin10NewsResource.FlashCategoryNumber));
        Assert.IsTrue(listPage.Items.All(item => item.NewsTime?.Year == 2026 && item.NewsTime?.Month == 9));
        Assert.AreEqual(listPage.Items.Count, listPage.Items.Select(item => item.NewsUrl).Distinct().Count());
    }

    [TestMethod]
    public void ParseRealOriginRoundTripToContent()
    {
        var spider = new Jin10NewsSpider();
        var listPage = Jin10NewsSpider.ParseListPage(LoadFixture("jin10_flash_page1.json"));

        foreach (var origin in listPage.InlineOrigins)
        {
            var parseResult = spider.ParseContent(origin.NewsOriginContent, origin.NewsUrl);

            Assert.AreEqual(origin.NewsUrl, parseResult.Content.NewsUrl);
            Assert.IsFalse(string.IsNullOrWhiteSpace(parseResult.Content.NewsTitle));
            // PLUS 专享条目正文为空 , 实现里用 vip_title 兜底 , 因此正文不应为空
            Assert.IsFalse(string.IsNullOrWhiteSpace(parseResult.Content.NewsContentText));
            Assert.IsNotNull(parseResult.Content.NewsTime);
        }
    }

    [TestMethod]
    public void ParseRealSecondPageContinuesOlderWithBoundaryRepeat()
    {
        var page1 = Jin10NewsSpider.ParseListPage(LoadFixture("jin10_flash_page1.json"));
        var page2 = Jin10NewsSpider.ParseListPage(LoadFixture("jin10_flash_page2.json"));

        // 接口的 max_time 是含边界语义 : 第二页会重复取回边界那一条 , 由入库去重吸收
        var oldestOfPage1 = page1.Items.Min(item => item.NewsTime);
        Assert.IsTrue(page2.Items.All(item => item.NewsTime <= oldestOfPage1));
        Assert.AreEqual(1, page1.Items.Select(item => item.NewsUrl).Intersect(page2.Items.Select(item => item.NewsUrl)).Count());
        Assert.IsNotNull(page1.NextCursor);
    }

    [TestMethod]
    public void PlusLockedItemFallbackToVipTitle()
    {
        // 真实响应里 PLUS 专享条目 content 为空、lock=true、标题在 vip_title
        var json = """
                   {"status":200,"message":"OK","data":[
                     {"id":"20260919110229436800","time":"2026-09-19 11:02:29","type":0,"important":0,
                      "data":{"content":"","lock":true,"exclusive_to":["plus"],"vip_desc":"PLUS专享快讯，解锁直达","vip_title":"据悉沙特阿拉伯苏丹空军基地遭到导弹袭击"},
                      "tags":[],"channel":[5]}
                   ]}
                   """;

        var spider = new Jin10NewsSpider();
        var listPage = Jin10NewsSpider.ParseListPage(json);
        var parseResult = spider.ParseContent(listPage.InlineOrigins[0].NewsOriginContent,
            listPage.InlineOrigins[0].NewsUrl);

        Assert.AreEqual("据悉沙特阿拉伯苏丹空军基地遭到导弹袭击", listPage.Items[0].NewsTitle);
        Assert.AreEqual("据悉沙特阿拉伯苏丹空军基地遭到导弹袭击", parseResult.Content.NewsContentText);
        Assert.AreEqual("https://flash.jin10.com/detail/20260919110229436800", listPage.Items[0].NewsUrl);
    }

    [TestMethod]
    public void ArticleTypeItemKeepTitleAndImage()
    {
        var json = """
                   {"status":200,"message":"OK","data":[
                     {"id":"20260919095159560800","time":"2026-09-19 09:51:59","type":2,"important":1,
                      "data":{"content":"本周你错过哪些刺激行情？","title":"一周热榜精选：沃什首次出手就是加息","pic":"https://gimg.jin10.com/gallary/26/08/demo.png/lite","tag":"周末盘点"},
                      "tags":["周末盘点"],"channel":[5]}
                   ]}
                   """;

        var spider = new Jin10NewsSpider();
        var listPage = Jin10NewsSpider.ParseListPage(json);
        var parseResult = spider.ParseContent(listPage.InlineOrigins[0].NewsOriginContent,
            listPage.InlineOrigins[0].NewsUrl);

        Assert.AreEqual("一周热榜精选：沃什首次出手就是加息", parseResult.Content.NewsTitle);
        Assert.AreEqual("周末盘点", parseResult.Content.NewsKeyword);
        Assert.AreEqual(1, parseResult.Images.Count);
        Assert.AreEqual("demo.png", parseResult.Images[0].ImageName);
    }

    [TestMethod]
    public void ParseListPageThrowOnErrorStatus()
    {
        Assert.ThrowsExactly<HtmlFormException>(() =>
            Jin10NewsSpider.ParseListPage("""{"status":502,"message":"bad gateway"}"""));
    }
}
