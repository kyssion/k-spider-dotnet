using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.News.Web.Jin10;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     金十「市场参考」文章源 ( 网页型管线 ) 解析回归 : 全部基于 TestData 里的真实接口响应 , 离线执行。
///     夹具 : jin10_article_list_28/30/53.json ( 综合/早餐/头条三栏目首页 ) +
///     jin10_article_detail.json ( 免费长文 , 正文 h2/img/p/strong ) +
///     jin10_article_detail_rich.json ( 纯 figure 图片文 )。
///     重抓夹具时须同步这里依赖固定值的断言 ( 标题 / display_datetime / 条目数 )。
/// </summary>
[TestClass]
public class Jin10ArticleSpiderTest
{
    private static string ReadFixture(string fileName)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
    }

    [TestMethod]
    public void ParseListPageMapsFields()
    {
        var page = Jin10ArticleSpider.ParseListPage(ReadFixture("jin10_article_list_53.json"), 405, 20, 1);

        Assert.AreEqual(20, page.Items.Count);
        Assert.AreEqual("2", page.NextCursor, "满页应翻下一页 ( 页码游标 )");
        var first = page.Items[0];
        Assert.AreEqual("https://xnews.jin10.com/details/231303", first.NewsUrl, "URL 用详情页规范形态");
        Assert.AreEqual("特朗普周二会见AI巨头，OpenAI同日开发者大会料推常驻AI智能体", first.NewsTitle);
        Assert.AreEqual("郑尧", first.NewsFrom, "以作者昵称署名");
        Assert.AreEqual((int)FromTypeOfNews.Jin10Media, first.FromMedia);
        Assert.AreEqual(405, first.Category);
        Assert.AreEqual(new DateTime(2026, 9, 29, 17, 32, 6), first.NewsTime);
        Assert.IsFalse(string.IsNullOrEmpty(first.NewsSummary), "introduction 应进入摘要");
        Assert.IsFalse(first.IsPaid, "免费条目 is_paid=false");
    }

    [TestMethod]
    public void ParseListPageSkipsVipItemsAndKeepsPaging()
    {
        // 综合流首页实测 20 条含 6 条付费专享 ( 如 231298 vip=1/super_vip=1 )
        var page = Jin10ArticleSpider.ParseListPage(ReadFixture("jin10_article_list_28.json"), 402, 20, 1);

        Assert.AreEqual(14, page.Items.Count, "付费专享条目 ( 匿名无正文 ) 应被跳过");
        Assert.IsFalse(page.Items.Any(item => item.IsPaid));
        Assert.IsFalse(page.Items.Any(item => item.NewsUrl?.EndsWith("/231298") == true), "付费样本 231298 不应入库");
        // 末页判断用原始条数 : 被跳过条目吃掉的满页不能误判成末页
        Assert.AreEqual("2", page.NextCursor);
    }

    [TestMethod]
    public void ParseListPageSkipsNonNewsItems()
    {
        // 视频卡片与列表项同形态 ( 仅 type 不同 ) , 用真实形态构造的载荷回归过滤分支
        const string response = """
                                {"data":{"list":[
                                  {"id":231300,"title":"视频卡片","introduction":"","display_datetime":"2026-09-29 12:00:00",
                                   "type":"video","vip":0,"super_vip":0,"elite_vip":0,"author":{"nick":"金十"}}
                                ],"page":1,"page_size":20,"total":1},"status":200}
                                """;

        var page = Jin10ArticleSpider.ParseListPage(response, 402, 20, 1);

        Assert.AreEqual(0, page.Items.Count, "非 news 形态 ( 视频/音频卡片 ) 无文章正文 , 跳过");
    }

    [TestMethod]
    public void ParseListPageStopsOnShortPage()
    {
        // 用大于实际条数的页长请求 , 短页即末页
        var page = Jin10ArticleSpider.ParseListPage(ReadFixture("jin10_article_list_30.json"), 403, 50, 1);

        Assert.IsNull(page.NextCursor, "短页即末页");
    }

    [TestMethod]
    public void ParseDetailMapsContentAndImages()
    {
        var spider = new Jin10ArticleSpider();
        var result = spider.ParseContent(ReadFixture("jin10_article_detail.json"),
            "https://xnews.jin10.com/details/231303");

        var content = result.Content;
        Assert.AreEqual("特朗普周二会见AI巨头，OpenAI同日开发者大会料推常驻AI智能体", content.NewsTitle);
        Assert.AreEqual("郑尧", content.NewsFrom);
        Assert.AreEqual(new DateTime(2026, 9, 29, 17, 32, 6), content.NewsTime);
        Assert.IsFalse(string.IsNullOrEmpty(content.NewsContentText), "纯文本聚合不应为空");
        // 正文实测含 h2/img/p/strong : 图片按出现顺序进图片列表
        Assert.IsTrue(result.Images.Count > 0, "正文图片应进图片列表");
        Assert.IsTrue(result.Images.All(image => image.NewsUrl == "https://xnews.jin10.com/details/231303"));
    }

    [TestMethod]
    public void ParseDetailExtractsFigureImage()
    {
        var spider = new Jin10ArticleSpider();
        var result = spider.ParseContent(ReadFixture("jin10_article_detail_rich.json"),
            "https://xnews.jin10.com/details/231299");

        // 纯 figure 图片文 : 图片与图注各自成段
        Assert.AreEqual(1, result.Images.Count);
        Assert.AreEqual("https://img.jin10.com/news/26/09/niiZHAXe0vvPmxQQO6LN0.jpg",
            result.Images[0].ImageResourceUrl);
    }

    [TestMethod]
    public void BadEnvelopeThrows()
    {
        var spider = new Jin10ArticleSpider();
        // 详情 : 内容下架/不存在时接口返回 status=404 信封 ( HTTP 仍 200 )
        Assert.ThrowsExactly<DownloadHttpRequestException>(() =>
            spider.ParseContent("""{"message":"内容不存在","status":404}""",
                "https://xnews.jin10.com/details/1"));
        // 详情 : 非法 JSON 被解析层统一包成 HtmlFormException
        Assert.ThrowsExactly<HtmlFormException>(() =>
            spider.ParseContent("not-json", "https://xnews.jin10.com/details/1"));
        // 列表 : 栏目下架同款信封
        Assert.ThrowsExactly<HtmlFormException>(() =>
            Jin10ArticleSpider.ParseListPage("""{"message":"内容已下架","status":404}""", 402, 20, 1));
    }

    [TestMethod]
    public void ReadIsPaidReadsDetailVipFlag()
    {
        Assert.IsFalse(Jin10ArticleSpider.ReadIsPaid(ReadFixture("jin10_article_detail.json")));
        Assert.IsTrue(Jin10ArticleSpider.ReadIsPaid(
            """{"data":{"vip":1,"super_vip":0,"elite_vip":0},"status":200}"""), "vip 三标记任一非零即付费");
        // 坏载荷按非付费处理 , 不拦截入库流程
        Assert.IsFalse(Jin10ArticleSpider.ReadIsPaid("not-json"));
    }
}
