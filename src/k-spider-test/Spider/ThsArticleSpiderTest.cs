using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.News.Web.Ths;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     同花顺文章源 ( 网页型管线 ) 解析回归 : 全部基于 TestData 里的真实页面 , 离线执行。
///     夹具 : ths_article_list_today.html / _p2.html ( 财经要闻栏目 SSR 列表 ,
///     原页为 GBK , 夹具已按响应头 charset 解码转存 UTF-8 , 模拟 HttpClient 解码产物 ) +
///     ths_article_detail.html ( 详情页 UTF-8 SSR 原样 )。
///     重抓夹具时须同步这里依赖固定值的断言 ( 条数 / 标题 / 时间 )。
/// </summary>
[TestClass]
public class ThsArticleSpiderTest
{
    private const int Category = 502;

    private static string ReadFixture(string fileName)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
    }

    [TestMethod]
    public void ParseListPageMapsFields()
    {
        var page = ThsArticleSpider.ParseListPage(ReadFixture("ths_article_list_today.html"), Category, 1);

        // 主列表区固定 25 条 ( 侧栏推荐链接不得混入 )
        Assert.AreEqual(25, page.Items.Count);
        Assert.AreEqual("2", page.NextCursor, "满页 25 条应翻下一页 ( 页码游标 )");
        var first = page.Items[0];
        Assert.AreEqual((int)FromTypeOfNews.ThsMedia, first.FromMedia);
        Assert.AreEqual(Category, first.Category);
        Assert.AreEqual("https://news.10jqka.com.cn/20260930/c680400243.shtml", first.NewsUrl);
        Assert.AreEqual("央行“四箭齐发”，带来哪些利好", first.NewsTitle);
        Assert.AreEqual("同花顺", first.NewsFrom, "列表无来源字段 , 回退平台名");
        Assert.AreEqual(new DateTime(2026, 9, 30, 13, 10, 0), first.NewsTime, "时刻无年份 , 年份从 URL 日期路径补全");
        Assert.IsTrue(page.Items.All(item => item.Category == Category));
    }

    [TestMethod]
    public void ParseListPageTwoPagesHaveNoOverlapAndCursorAdvances()
    {
        var page1 = ThsArticleSpider.ParseListPage(ReadFixture("ths_article_list_today.html"), Category, 1);
        var page2 = ThsArticleSpider.ParseListPage(ReadFixture("ths_article_list_today_p2.html"), Category, 2);

        var overlap = page1.Items.Select(item => item.NewsUrl)
            .Intersect(page2.Items.Select(item => item.NewsUrl)).Count();
        Assert.AreEqual(0, overlap, "index_2 分页两页不应重叠");
        Assert.AreEqual("3", page2.NextCursor, "页2 满页应继续翻页");
        Assert.IsTrue(page2.Items.All(item => item.NewsTime <= page1.Items.Min(item => item.NewsTime)),
            "页2 条目应不晚于页1 最老条目");
    }

    [TestMethod]
    public void ParseListPageThrowsOnMissingContainer()
    {
        Assert.ThrowsExactly<HtmlFormException>(() =>
            ThsArticleSpider.ParseListPage("<html><body>其它页面</body></html>", Category, 1));
    }

    [TestMethod]
    public void ParseContentExtractsParagraphsAndTime()
    {
        var spider = new ThsArticleSpider();
        var result = spider.ParseContent(ReadFixture("ths_article_detail.html"),
            "https://news.10jqka.com.cn/20260930/c680400243.shtml");

        var content = result.Content;
        Assert.AreEqual("央行“四箭齐发”，带来哪些利好", content.NewsTitle, "h1 标题");
        Assert.AreEqual("同花顺", content.NewsFrom);
        Assert.AreEqual(new DateTime(2026, 9, 30, 13, 10, 0), content.NewsTime, "详情页时间 yyyy-MM-dd HH:mm");
        Assert.IsFalse(string.IsNullOrWhiteSpace(content.NewsContentText), "纯文本聚合不应为空");
        Assert.IsTrue(content.NewsContentText!.Contains("抵押补充贷款"), "正文段落完整保留");
        Assert.IsNotNull(content.NewsContentJson);
        Assert.IsFalse(content.NewsContentJson!.Contains('<'), "片段不应残留 HTML 标签");
    }

    [TestMethod]
    public void ParseContentThrowsOnMissingRegion()
    {
        var spider = new ThsArticleSpider();
        Assert.ThrowsExactly<DownloadHttpRequestException>(() =>
            spider.ParseContent("<html><body>其它页面</body></html>", "https://news.10jqka.com.cn/x.shtml"));
    }
}
