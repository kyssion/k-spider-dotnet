using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.News.Web.Nbd;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     每经文章源 ( 网页型管线 ) 解析回归 : 全部基于 TestData 里的真实页面 , 离线执行。
///     夹具 : nbd_article_list_toutiao.html ( 每经头条栏目 SSR 列表 ) +
///     nbd_article_detail.html ( 详情页 UTF-8 SSR 原样 )。
///     重抓夹具时须同步这里依赖固定值的断言 ( 条数 / 标题 / 时间 )。
/// </summary>
[TestClass]
public class NbdArticleSpiderTest
{
    private const int Category = 701;

    private static string ReadFixture(string fileName)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
    }

    [TestMethod]
    public void ParseListPageMapsFields()
    {
        var page = NbdArticleSpider.ParseListPage(ReadFixture("nbd_article_list_toutiao.html"), Category);

        // 夹具实测 80 个锚点 → 去重后 77 条 ( 页内重复锚点吸收 ) ; 整页即全量无翻页
        Assert.IsTrue(page.Items.Count is >= 70 and <= 80, $"条数异常 : {page.Items.Count}");
        Assert.IsNull(page.NextCursor, "栏目页整页即全量 , 无翻页游标");
        Assert.IsTrue(page.Items.All(item => item.NewsUrl!.Contains("nbd.com.cn/articles/")), "存在非文章链接混入");
        Assert.IsTrue(page.Items.Select(item => item.NewsUrl).Distinct().Count() == page.Items.Count,
            "news_url 应已去重");
        Assert.IsTrue(page.Items.Count > 0, "不应解析出空列表");
        var first = page.Items[0];
        Assert.AreEqual((int)FromTypeOfNews.NbdMedia, first.FromMedia);
        Assert.AreEqual(Category, first.Category);
        Assert.AreEqual("每日经济新闻", first.NewsFrom);
        Assert.AreEqual("九成动力电池回收企业将出局？从“二手复用”到“回炉重炼”，近20万家企业仅300家握核", first.NewsTitle);
        Assert.AreEqual(new DateTime(2026, 9, 29), first.NewsTime, "时间取链接日期当日 00:00");
        Assert.AreEqual("https://www.nbd.com.cn/articles/2026-09-29/4595080.html", first.NewsUrl);
    }

    [TestMethod]
    public void ParseListPageSkipsNonArticleAnchors()
    {
        const string html = """
                            <html><body>
                            <a href="https://www.nbd.com.cn/columns/1161/">栏目链接</a>
                            <a href="https://www.nbd.com.cn/articles/2026-09-29/4595080.html">九成动力电池回收企业将出局？</a>
                            <a href="https://www.nbd.com.cn/articles/2026-09-29/4595080.html">九成动力电池回收企业将出局？( 页内重复 )</a>
                            <a href="https://www.nbd.com.cn/articles/tag/x.html">缺日期路径</a>
                            </body></html>
                            """;

        var page = NbdArticleSpider.ParseListPage(html, Category);

        Assert.AreEqual(1, page.Items.Count, "栏目链接 / 页内重复 / 缺日期路径应被排除或去重");
    }

    [TestMethod]
    public void ParseListPageThrowsOnEmptyPage()
    {
        Assert.ThrowsExactly<HtmlFormException>(() =>
            NbdArticleSpider.ParseListPage("<html><body></body></html>", Category));
    }

    [TestMethod]
    public void ParseContentExtractsParagraphsAndTime()
    {
        var spider = new NbdArticleSpider();
        var result = spider.ParseContent(ReadFixture("nbd_article_detail.html"),
            "https://www.nbd.com.cn/articles/2026-09-29/4595080.html");

        var content = result.Content;
        Assert.AreEqual("每日经济新闻", content.NewsFrom);
        Assert.AreEqual(new DateTime(2026, 9, 29, 22, 44, 43), content.NewsTime, "详情页时间精确到秒");
        Assert.IsFalse(string.IsNullOrWhiteSpace(content.NewsContentText), "纯文本聚合不应为空");
        Assert.IsNotNull(content.NewsContentJson);
        Assert.IsFalse(content.NewsContentJson!.Contains('<'), "片段不应残留 HTML 标签");
        Assert.IsFalse(string.IsNullOrWhiteSpace(content.NewsTitle), "标题不应为空");
    }

    [TestMethod]
    public void ParseContentThrowsOnMissingEditor()
    {
        var spider = new NbdArticleSpider();
        Assert.ThrowsExactly<DownloadHttpRequestException>(() =>
            spider.ParseContent("<html><body>其它页面</body></html>", "https://www.nbd.com.cn/articles/x.html"));
    }
}
