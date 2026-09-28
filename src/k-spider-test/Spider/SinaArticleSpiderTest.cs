using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.News;
using KSpider.Spider.News.Flash.Sina;
using KSpider.Spider.News.Web.Sina;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     新浪财经文章源 ( 网页型管线 ) 解析回归 : 全部基于 TestData 里的真实接口响应 , 离线执行。
///     夹具 : sina_article_roll_page1/2.json ( 财经滚动接口两页 ) + sina_article_column_56592.html ( 上市公司栏目页 ) +
///     sina_article_detail.html / sina_article_detail_rich.html ( 详情页 , 后者含 blockquote 引用块 )。
///     重抓夹具时须同步这里依赖固定值的断言 ( 标题 / ctime / 游标 )。
/// </summary>
[TestClass]
public class SinaArticleSpiderTest
{
    private const string FirstDetailUrl = "https://finance.sina.com.cn/stock/usstock/c/2026-09-29/doc-initmeav9622448.shtml";

    private static string ReadFixture(string fileName)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
    }

    [TestMethod]
    public void ParseRollListPageMapsFieldsAndCategory()
    {
        var page = SinaArticleSpider.ParseRollListPage(ReadFixture("sina_article_roll_page1.json"), 202, 50, 1);

        Assert.IsTrue(page.Items.Count > 0, "滚动接口未解析出任何条目");
        var first = page.Items[0];
        Assert.AreEqual(FirstDetailUrl, first.NewsUrl);
        Assert.AreEqual("财报前夕，美光“超级多头”重申2000美元目标价", first.NewsTitle);
        Assert.AreEqual("环球市场播报", first.NewsFrom);
        Assert.AreEqual((int)FromTypeOfNews.SinaMedia, first.FromMedia);
        Assert.AreEqual(202, first.Category);
        // ctime=1790613243 → 东八区 2026-09-29 00:34:03 ( 与详情页展示时间 00:34 一致 )
        Assert.AreEqual(new DateTime(2026, 9, 29, 0, 34, 3), first.NewsTime);
        Assert.IsFalse(string.IsNullOrEmpty(first.NewsSummary), "导语 ( intro ) 应进入摘要");
    }

    [TestMethod]
    public void ParseRollListPageCursorIsNextPageNumber()
    {
        // 页码翻页 : 按 ctime 严格降序、页间不重叠 ( 实测两页 50 条零重叠 ) ;
        // 满页继续给下一页游标 , 短页即末页
        var page1 = SinaArticleSpider.ParseRollListPage(ReadFixture("sina_article_roll_page1.json"), 202, 50, 1);
        var page2 = SinaArticleSpider.ParseRollListPage(ReadFixture("sina_article_roll_page2.json"), 202, 50, 2);

        Assert.AreEqual(50, page1.Items.Count);
        Assert.AreEqual("2", page1.NextCursor, "满页应给出下一页页码游标");
        Assert.AreEqual(50, page2.Items.Count);
        Assert.AreEqual("3", page2.NextCursor);

        var urls1 = page1.Items.Select(item => item.NewsUrl).ToHashSet();
        var urls2 = page2.Items.Select(item => item.NewsUrl).ToHashSet();
        Assert.AreEqual(0, urls1.Intersect(urls2).Count(), "页码翻页两页不应重叠");

        var oldestOfPage1 = page1.Items.Min(item => item.NewsTime);
        Assert.IsTrue(page2.Items.All(item => item.NewsTime <= oldestOfPage1), "第二页出现了比第一页更新的数据");
    }

    [TestMethod]
    public void ParseColumnPageMapsFieldsAndDerivesYearFromUrl()
    {
        // 栏目页时间只有 "(09月28日 23:54)" , 年份从条目 URL 路径 /2026-09-28/doc- 补全
        var page = SinaArticleSpider.ParseColumnPage(ReadFixture("sina_article_column_56592.html"), 208);

        Assert.IsTrue(page.Items.Count > 0, "栏目页未解析出任何条目");
        Assert.IsNull(page.NextCursor, "栏目滚动页整页即完整列表 , 没有下一页");

        var first = page.Items[0];
        Assert.AreEqual("https://finance.sina.com.cn/stock/s/2026-09-28/doc-initmear8308661.shtml", first.NewsUrl);
        Assert.AreEqual("准万亿城市“卡位战”，悬念再起", first.NewsTitle);
        Assert.AreEqual(new DateTime(2026, 9, 28, 23, 54, 0), first.NewsTime);
        Assert.AreEqual((int)FromTypeOfNews.SinaMedia, first.FromMedia);
        Assert.AreEqual(208, first.Category);
        Assert.AreEqual(SinaNewsResource.NewsFromName, first.NewsFrom, "列表页没有来源字段 , 应回退平台名");

        // 夹具实测 200 条 li 里混有 14 条无日期路径的条目 , 应全部被跳过
        Assert.AreEqual(186, page.Items.Count);
    }

    [TestMethod]
    public void ParseColumnPageSkipsItemsWithoutDateOrDocUrl()
    {
        // 手工构造栏目页 , 锁定三类跳过分支 : 非文章页 ( 无 doc- ) / URL 无日期路径 / 缺时间括号
        const string html = """
                            <html><body><ul id="listcontent">
                            <li><a href="https://finance.sina.com.cn/stock/s/2026-09-28/doc-initmear8308661.shtml">正常条目</a><span>(09月28日 23:54)</span></li>
                            <li><a href="https://finance.sina.com.cn/zt_d/special/">专题链接无详情页</a><span>(09月28日 23:00)</span></li>
                            <li><a href="https://finance.sina.com.cn/roll/doc-initmear8308.shtml">无日期路径</a><span>(09月28日 23:00)</span></li>
                            <li><a href="https://finance.sina.com.cn/stock/s/2026-09-28/doc-initmear8308661.shtml">缺时间括号</a></li>
                            </ul></body></html>
                            """;

        var page = SinaArticleSpider.ParseColumnPage(html, 208);

        Assert.AreEqual(1, page.Items.Count, "非文章页 / 无日期路径 / 缺时间的条目应被跳过");
        Assert.AreEqual("正常条目", page.Items[0].NewsTitle);
    }

    [TestMethod]
    public void ParseContentMapsDetailFieldsSegmentsAndImages()
    {
        var spider = new SinaArticleSpider();

        var result = spider.ParseContent(ReadFixture("sina_article_detail.html"), FirstDetailUrl);

        var content = result.Content;
        Assert.AreEqual("财报前夕，美光“超级多头”重申2000美元目标价", content.NewsTitle);
        Assert.AreEqual("环球市场播报", content.NewsFrom);
        Assert.AreEqual(new DateTime(2026, 9, 29, 0, 34, 0), content.NewsTime);
        Assert.AreEqual("财报前夕，美光“超级多头”重申2000美元目标价", content.NewsKeyword, "meta keywords 应落入 news_keyword");

        Assert.IsFalse(string.IsNullOrEmpty(content.NewsContentText), "纯文本聚合不应为空");
        Assert.IsTrue(content.NewsContentText!.Contains("美光"), "纯文本应包含正文内容");
        Assert.IsNotNull(content.NewsContentJson);
        Assert.IsTrue(content.NewsContentJson!.Contains(NewsContentSegment.ImgType), "片段中应包含图片");
        // 夹具正文恰好 1 张内容图 ( div.img_wrapper ) ; 文末 appendQr 推广二维码不进图片列表
        Assert.AreEqual(1, result.Images.Count);
        Assert.IsTrue(result.Images.All(image => image.NewsUrl == FirstDetailUrl));
        Assert.IsTrue(result.Images[0].ImageResourceUrl!.Contains("n.sinaimg.cn"));
    }

    [TestMethod]
    public void ParseContentKeepsBlockquoteText()
    {
        // 真实夹具 ( 奥多比假日季购物 ) 正文含 blockquote 引用块 : 引用是内容的一部分 ,
        // 不能按"未知标签"丢弃 ( 与财联社解析器同款约定 )
        var spider = new SinaArticleSpider();
        var url = "https://finance.sina.com.cn/stock/usstock/c/2026-09-28/doc-initmeau2811260.shtml";

        var result = spider.ParseContent(ReadFixture("sina_article_detail_rich.html"), url);

        Assert.AreEqual("奥多比预测今年美国假日季线上购物将创历史新高", result.Content.NewsTitle);
        Assert.IsTrue(result.Content.NewsContentJson!.Contains("blockquote"), "片段 TagType 应保留引用块溯源标记");
        Assert.IsTrue(result.Content.NewsContentText!.Contains("假日季"), "正文文本应可正常聚合");
    }

    [TestMethod]
    public void ParseContentThrowsOnBrokenOrigin()
    {
        var spider = new SinaArticleSpider();
        // 缺正文容器 : 页面模板缺失或被拦
        Assert.ThrowsExactly<DownloadHttpRequestException>(() =>
            spider.ParseContent("<html><body>not a article page</body></html>", FirstDetailUrl));
        // 有正文容器但发布时间无法解析 ( 无 span.date 也无 article:published_time )
        Assert.ThrowsExactly<HtmlFormException>(() =>
            spider.ParseContent("""<html><body><div id="artibody"><p>正文</p></div></body></html>""", FirstDetailUrl));
    }
}
