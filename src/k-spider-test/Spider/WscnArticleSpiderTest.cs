using System.Text.Json.Nodes;
using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.News.Web.Wscn;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     华尔街见闻文章源 ( 网页型管线 ) 解析回归 : 全部基于 TestData 里的真实接口响应 , 离线执行。
///     夹具 : wscn_article_list_page1/2.json ( 全量文章流两页 ) + wscn_article_detail.json ( 免费长文 ) +
///     wscn_article_detail_paid.json ( 付费截断文 )。
///     重抓夹具时须同步这里依赖固定值的断言 ( 标题 / display_time / 游标 )。
/// </summary>
[TestClass]
public class WscnArticleSpiderTest
{
    private const string FirstArticleUrl = "https://wallstreetcn.com/articles/3782720";

    private static string ReadFixture(string fileName)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
    }

    [TestMethod]
    public void ParseListPageMapsFieldsAndInfersCategory()
    {
        var page = WscnArticleSpider.ParseListPage(ReadFixture("wscn_article_list_page1.json"), 30);

        Assert.AreEqual(30, page.Items.Count);
        var first = page.Items[0];
        Assert.AreEqual(FirstArticleUrl, first.NewsUrl);
        Assert.AreEqual("加入个人AI Agent大战！豆包被曝将推个人助理产品“Spell”，4月已内测", first.NewsTitle);
        Assert.AreEqual("卜淑情", first.NewsFrom, "见闻文章以专职作者署名");
        Assert.AreEqual((int)FromTypeOfNews.WscnMedia, first.FromMedia);
        // 首条 categories=["global"] → 302 要闻
        Assert.AreEqual(302, first.Category);
        // display_time=1790663006 → 东八区 2026-09-29 14:23:26
        Assert.AreEqual(new DateTime(2026, 9, 29, 14, 23, 26), first.NewsTime);
        Assert.IsFalse(string.IsNullOrEmpty(first.NewsSummary), "content_short 应进入摘要");
        Assert.IsFalse(first.IsPaid, "免费条目 is_priced=false");
    }

    [TestMethod]
    public void ListMarksPaidItemsAndNormalizesUrl()
    {
        var page = WscnArticleSpider.ParseListPage(ReadFixture("wscn_article_list_page1.json"), 30);
        var paid = page.Items.Where(item => item.IsPaid).ToList();

        // 夹具第 25/26 条为同一篇付费文 ( is_priced=true , uri 带 ?layout= 查询串 )
        Assert.AreEqual("https://wallstreetcn.com/premium/articles/3782635", paid[0].NewsUrl,
            "付费条目 uri 的可变查询串应被剥掉 ( 去重键稳定形态 )");
        Assert.IsTrue(paid.All(item => item.NewsUrl?.IndexOf('?') < 0), "入库 URL 不应带查询串");
    }

    [TestMethod]
    public void ReadIsPaidReadsDetailFlag()
    {
        Assert.IsFalse(WscnArticleSpider.ReadIsPaid(ReadFixture("wscn_article_detail.json")));
        Assert.IsTrue(WscnArticleSpider.ReadIsPaid(ReadFixture("wscn_article_detail_paid.json")));
        // 坏载荷按非付费处理 , 不拦截入库流程
        Assert.IsFalse(WscnArticleSpider.ReadIsPaid("not-json"));
    }

    [TestMethod]
    public void CategoryInferencePrefersVerticalOverBroad()
    {
        // 多标签条目按优先级落垂直分类 : ai 优先于 us-shares , shares 宽泛标签兜底 , 全不命中归要闻
        Assert.AreEqual(309, WscnArticleResource.InferCategory(["global", "us-shares", "ai"]));
        Assert.AreEqual(304, WscnArticleResource.InferCategory(["global", "us-shares"]));
        Assert.AreEqual(303, WscnArticleResource.InferCategory(["shares", "global"]));
        Assert.AreEqual(309, WscnArticleResource.InferCategory(["enterprise", "ai"]), "AI 科技规则在表序上先于产业公司");
        Assert.AreEqual(311, WscnArticleResource.InferCategory(["wscn-ipo", "shares"]));
        Assert.AreEqual(302, WscnArticleResource.InferCategory(["global"]));
        Assert.AreEqual(302, WscnArticleResource.InferCategory(null), "无标签归要闻");
    }

    [TestMethod]
    public void CursorContinuesToOlderPageWithoutOverlap()
    {
        // 游标为接口给的 next_cursor ( "最新,最老" 时间对 ) , 单调向旧、页间零重叠 , 短页即末页
        var page1 = WscnArticleSpider.ParseListPage(ReadFixture("wscn_article_list_page1.json"), 30);
        var page2 = WscnArticleSpider.ParseListPage(ReadFixture("wscn_article_list_page2.json"), 30);

        Assert.AreEqual("1790663006,1790651700", page1.NextCursor, "满页应透传接口 next_cursor");
        Assert.AreEqual(30, page2.Items.Count);
        Assert.AreEqual("1790663006,1790643238", page2.NextCursor);

        Assert.IsTrue(page2.Items.Max(item => item.NewsTime!.Value) <= page1.Items.Min(item => item.NewsTime!.Value),
            "第二页应整体更旧");
        var urls1 = page1.Items.Select(item => item.NewsUrl).ToHashSet();
        var urls2 = page2.Items.Select(item => item.NewsUrl).ToHashSet();
        Assert.AreEqual(0, urls1.Intersect(urls2).Count(), "游标翻页两页不应重叠");
    }

    [TestMethod]
    public void ParseDetailMapsContentSegmentsAndImages()
    {
        var spider = new WscnArticleSpider();
        var result = spider.ParseContent(ReadFixture("wscn_article_detail.json"), FirstArticleUrl);

        // 免费长文 : 详情字段 + 结构化正文 + 图片 ( display_time=1790654990 → 东八区 09-29 13:23 )
        Assert.AreEqual("“用户日增速10%”！23岁天才辍学生造出Meta Muse最大劲敌，14人团队撑起671亿估值",
            result.Content.NewsTitle);
        Assert.AreEqual("龙玥", result.Content.NewsFrom);
        Assert.AreEqual(new DateTime(2026, 9, 29, 12, 9, 50), result.Content.NewsTime);
        Assert.IsTrue(result.Content.NewsContentText!.Length > 1000, "长文纯文本应完整 ( 实测约 3700 字 )");
        Assert.IsTrue(result.Images.Count >= 1, "正文图片应被提取");
        StringAssert.Contains(result.Content.NewsContentJson, "TEXT", "结构化片段应序列化进 JSON");
    }

    [TestMethod]
    public void ParsePaidDetailStillParsesTruncatedBody()
    {
        // 付费文 ( is_priced ) 详情正文被截断 ( 实测约 700 字 ) , 但结构完整仍可解析入库
        var spider = new WscnArticleSpider();
        var result = spider.ParseContent(ReadFixture("wscn_article_detail_paid.json"),
            "https://wallstreetcn.com/articles/3782635");

        Assert.AreEqual("6.5美元/加仑历史新高！美国柴油禁运是否进入倒计时？", result.Content.NewsTitle);
        Assert.IsTrue(result.Content.NewsContentText!.Length > 300, "截断预览仍应有足量文本");
        Assert.IsTrue(result.Images.Count >= 1, "付费文预览段的图片应保留");
    }

    [TestMethod]
    public void ParseListPageThrowsOnErrorStatusAndEmptyData()
    {
        // 接口错误码与 limit 超上限的静默空 data ( code 仍 20000 ) 都要显式报错 , 不当"没有新闻"处理
        Assert.ThrowsExactly<HtmlFormException>(() =>
            WscnArticleSpider.ParseListPage("""{"code":60327,"message":"extract 不正确","data":{}}""", 30));

        Assert.ThrowsExactly<HtmlFormException>(() =>
            WscnArticleSpider.ParseListPage("""{"code":20000,"message":"OK","data":""}""", 30),
            "limit 超上限时 data 为空字符串 , 必须与正常空页区分");
    }

    [TestMethod]
    public void ListSkipsNonArticleLayoutAndMissingFields()
    {
        // 坏数据路径 ( 手搓最小节点 ) : 视频布局 / 缺 uri / 缺时间的条目不入库
        var payload = """
        {
          "code": 20000, "message": "OK",
          "data": {
            "items": [
              { "uri": "https://wallstreetcn.com/articles/1", "title": "正常文章", "display_time": 1790663006,
                "content_short": "正文", "layout": "wscn-layout", "categories": ["global"], "author": {} },
              { "uri": "https://wallstreetcn.com/articles/2", "title": "视频卡片", "display_time": 1790663006,
                "content_short": "x", "layout": "video-layout", "categories": ["global"], "author": {} },
              { "uri": "", "title": "缺 uri", "display_time": 1790663006,
                "content_short": "x", "layout": "wscn-layout", "categories": [], "author": {} },
              { "uri": "https://wallstreetcn.com/articles/4", "title": "缺时间",
                "display_time": 0, "content_short": "x", "layout": "wscn-layout", "categories": [], "author": {} }
            ],
            "next_cursor": "1790663006,1790663006"
          }
        }
        """;
        var page = WscnArticleSpider.ParseListPage(payload, 4);

        Assert.AreEqual(1, page.Items.Count, "只有正常文章条目应入库");
        Assert.AreEqual("正常文章", page.Items[0].NewsTitle);
        Assert.IsNull(page.NextCursor, "1 < 4 短页即末页");
        // author 缺 display_name 时回退平台名
        Assert.AreEqual("华尔街见闻", page.Items[0].NewsFrom);
    }
}
