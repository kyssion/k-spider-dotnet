using KSpider.Exceptions;
using KSpider.Spider;
using KSpider.Spider.News.Flash.Jin10;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     金十数据快讯 : 真实抓取数据的快讯解析回归 ( 夹具为 2026-09-19 接口原样响应 , 测试离线 )
/// </summary>
[TestClass]
public class Jin10RealDataTest
{
    private static string LoadFixture(string fileName)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
    }

    [TestMethod]
    public void ParseRealPageMapEveryFlashRecord()
    {
        var page = Jin10NewsSpider.ParseFlashPage(LoadFixture("jin10_flash_page1.json"), 50);

        Assert.IsTrue(page.Items.Count > 0);
        Assert.IsTrue(page.Items.All(item => item.NewsUrl.StartsWith("https://flash.jin10.com/detail/")));
        Assert.IsTrue(page.Items.All(item => !string.IsNullOrEmpty(item.Title)));
        Assert.IsTrue(page.Items.All(item => !string.IsNullOrEmpty(item.Content)));
        Assert.IsTrue(page.Items.All(item => item.FromMedia == (int)FromTypeOfNews.Jin10Media));
        Assert.IsTrue(page.Items.All(item => item.Category == Jin10NewsResource.FlashCategoryNumber));
        Assert.IsTrue(page.Items.All(item => item.NewsTime.Year == 2026 && item.NewsTime.Month == 9));
        Assert.AreEqual(page.Items.Count, page.Items.Select(item => item.NewsUrl).Distinct().Count());
        Assert.IsNotNull(page.NextCursor);
    }

    [TestMethod]
    public void ParseRealLevelFromImportant()
    {
        var page = Jin10NewsSpider.ParseFlashPage(LoadFixture("jin10_flash_page1.json"), 50);

        // 真实夹具 21 条 : important=1 x7 ( level 2 重要 ) , 其余 level 1
        Assert.AreEqual(7, page.Items.Count(item => item.Level == 2));
        Assert.AreEqual(page.Items.Count - 7, page.Items.Count(item => item.Level == 1));
    }

    [TestMethod]
    public void ParseRealSecondPageContinuesOlderWithBoundaryRepeat()
    {
        var page1 = Jin10NewsSpider.ParseFlashPage(LoadFixture("jin10_flash_page1.json"), 50);
        var page2 = Jin10NewsSpider.ParseFlashPage(LoadFixture("jin10_flash_page2.json"), 50);

        // 接口的 max_time 是含边界语义 : 第二页会重复取回边界那一条 , 由入库 ON CONFLICT 吸收
        var oldestOfPage1 = page1.Items.Min(item => item.NewsTime);
        Assert.IsTrue(page2.Items.All(item => item.NewsTime <= oldestOfPage1));
        Assert.AreEqual(1,
            page1.Items.Select(item => item.NewsUrl).Intersect(page2.Items.Select(item => item.NewsUrl)).Count());
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

        var page = Jin10NewsSpider.ParseFlashPage(json, 50);

        var item = page.Items[0];
        Assert.AreEqual("据悉沙特阿拉伯苏丹空军基地遭到导弹袭击", item.Title);
        Assert.AreEqual("据悉沙特阿拉伯苏丹空军基地遭到导弹袭击", item.Content);
        Assert.AreEqual("https://flash.jin10.com/detail/20260919110229436800", item.NewsUrl);
    }

    [TestMethod]
    public void PlusLockedItemWithoutAnyPublicContentSkipped()
    {
        // 实时数据里存在 content 与 vip_title 都为空的 PLUS 锁定条目 ( 实测约 3/21 ) , 无任何公开信息 , 不入库
        var json = """
                   {"status":200,"message":"OK","data":[
                     {"id":"20260919110300000000000","time":"2026-09-19 11:03:00","type":0,"important":0,
                      "data":{"content":"","lock":true,"exclusive_to":["plus"],"vip_desc":"PLUS专享快讯，解锁直达","vip_title":""},
                      "tags":[],"channel":[5]},
                     {"id":"20260919110330400000000","time":"2026-09-19 11:03:30","type":0,"important":0,
                      "data":{"content":"正常条目正文","title":"","vip_title":"","pic":""},
                      "tags":[],"channel":[5]}
                   ]}
                   """;

        var page = Jin10NewsSpider.ParseFlashPage(json, 50);

        // 空条目被跳过 , 正常条目保留
        Assert.AreEqual(1, page.Items.Count);
        Assert.AreEqual("https://flash.jin10.com/detail/20260919110330400000000", page.Items[0].NewsUrl);
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

        var page = Jin10NewsSpider.ParseFlashPage(json, 50);

        var item = page.Items[0];
        Assert.AreEqual("一周热榜精选：沃什首次出手就是加息", item.Title);
        Assert.AreEqual("周末盘点", item.Keyword);
        Assert.AreEqual(2, item.Level);
        Assert.AreEqual("[\"https://gimg.jin10.com/gallary/26/08/demo.png/lite\"]", item.ImageUrls);
    }

    [TestMethod]
    public void ParseFlashPageThrowOnErrorStatus()
    {
        Assert.ThrowsExactly<HtmlFormException>(() =>
            Jin10NewsSpider.ParseFlashPage("""{"status":502,"message":"bad gateway"}""", 50));
    }
}
