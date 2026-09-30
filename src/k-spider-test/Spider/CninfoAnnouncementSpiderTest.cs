using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.Announcement.Cninfo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     巨潮公告源 ( Announcement 管线 ) 解析回归 : 全部基于 TestData 里的真实接口响应 , 离线执行。
///     夹具 : cninfo_announcement_page1.json ( 分类白名单 × 2026-09-29~30 窗口首页 30 条 )。
///     重抓夹具时须同步这里依赖固定值的断言 ( 首条 id / 标题 )。
/// </summary>
[TestClass]
public class CninfoAnnouncementSpiderTest
{
    private static string ReadFixture(string fileName)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
    }

    [TestMethod]
    public void ParseAnnouncementPageMapsFields()
    {
        var page = CninfoAnnouncementSpider.ParseAnnouncementPage(ReadFixture("cninfo_announcement_page1.json"));

        Assert.AreEqual(30, page.Items.Count, "pageSize 服务端钳 30");
        Assert.IsTrue(page.HasMore, "两日窗口 1034 条 , 首页后必有更多");
        var first = page.Items[0];
        Assert.AreEqual((int)FromTypeOfNews.CninfoMedia, first.FromMedia);
        Assert.AreEqual("1225589730", first.AnnouncementId, "站内唯一标识作去重键");
        Assert.AreEqual("第四届董事会第十六次会议决议公告", first.Title);
        Assert.AreEqual("300674", first.SecCode);
        Assert.AreEqual("宇信科技", first.SecName);
        Assert.AreEqual(new DateTime(2026, 9, 30, 15, 56, 7), first.PublishTime, "毫秒时间按东八区换算");
        Assert.AreEqual("http://static.cninfo.com.cn/finalpage/2026-09-30/1225589730.PDF", first.PdfUrl,
            "附件根地址拼接");
        Assert.IsNotNull(first.Category, "分类代码串原样入库");
        Assert.IsFalse(string.IsNullOrEmpty(first.RawContent));
        // 行键全局唯一
        Assert.AreEqual(page.Items.Count, page.Items.Select(item => item.AnnouncementId).Distinct().Count());
    }

    [TestMethod]
    public void ParseAnnouncementPageStripsHighlightTags()
    {
        // isHLtitle=true 时标题含 <em> 高亮标签 , 解析应剥掉
        const string response = """
                                {"announcements":[{"announcementId":"100","announcementTitle":"关于<em>股价</em>异常波动的公告",
                                  "secCode":"600000","secName":"浦发银行","announcementTime":1790754967000,
                                  "adjunctUrl":"finalpage/2026-09-30/100.PDF"}],"hasMore":false}
                                """;

        var page = CninfoAnnouncementSpider.ParseAnnouncementPage(response);

        var item = page.Items.Single();
        Assert.AreEqual("关于股价异常波动的公告", item.Title, "高亮标签应剥除");
        Assert.IsFalse(page.HasMore, "hasMore=false 无下一页");
    }

    [TestMethod]
    public void ParseAnnouncementPageThrowsOnMissingArray()
    {
        Assert.ThrowsExactly<HtmlFormException>(() =>
            CninfoAnnouncementSpider.ParseAnnouncementPage("""{"hasMore":false}"""));
        Assert.ThrowsExactly<HtmlFormException>(() =>
            CninfoAnnouncementSpider.ParseAnnouncementPage("not json"));
    }

    [TestMethod]
    public void ParseAnnouncementPageSkipsBrokenItems()
    {
        const string response = """
                                {"announcements":[
                                  {"announcementId":"","announcementTitle":"缺 id","secCode":"600000"},
                                  {"announcementId":"200","announcementTitle":"","secCode":"600001"},
                                  {"announcementId":"201","announcementTitle":"无证券主体公告 ( 基金类 )","secCode":"","secName":""}
                                ],"hasMore":false}
                                """;

        var page = CninfoAnnouncementSpider.ParseAnnouncementPage(response);

        Assert.AreEqual(1, page.Items.Count, "缺 id / 缺标题的坏数据应跳过");
        Assert.IsNull(page.Items[0].SecCode, "无证券主体公告证券列可空 ( 不跳过 )");
    }
}
