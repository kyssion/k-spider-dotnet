using KSpider.Exceptions;
using KSpider.Spider;
using KSpider.Spider.ClsNews;
using KSpider.Spider.News;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     财联社电报源测试 : 签名向量 / 列表解析 / 游标 / 注册 ( 全部离线 , 不访问网络 )
/// </summary>
[TestClass]
public class ClsNewsSpiderTest
{
    // 实测向量 : 请求 rn=20 时前端实际使用的 query 与签名 ( 2026-09-19 抓包核对 )
    private const string VerifiedQueryString =
        "app=CailianpressWeb&last_time=0&os=web&refresh_type=1&rn=20&sv=8.7.9";
    private const string VerifiedSign = "e11ef7d616d8f9a2f056e6df1aefc4d4";

    // 带标题 + 图片的电报
    private const string TitleItemJson = """
                                        {
                                          "id": 2487643,
                                          "title": "国际原子能机构新增11个理事会成员国",
                                          "brief": "【国际原子能机构新增11个理事会成员国】财联社9月19日电，11个新当选国家将加入国际原子能机构理事会。",
                                          "content": "【国际原子能机构新增11个理事会成员国】财联社9月19日电，11个新当选国家将加入国际原子能机构理事会，任期为2026年至2028年。",
                                          "ctime": 1789752848,
                                          "img": "https://image.cls.cn/images/20260919/cover.png",
                                          "images": ["https://image.cls.cn/images/20260919/body.png"],
                                          "subjects": [{"subject_id": 1556, "subject_name": "环球市场情报"}, {"subject_id": 1557, "subject_name": "美股IPO动态"}]
                                        }
                                        """;

    // 无标题电报 ( 用摘要兜底 )
    private const string UntitledItemJson = """
                                            {
                                              "id": 2487644,
                                              "title": "",
                                              "brief": "财联社9月19日电，消息人士称，西屋电气计划在美国IPO中寻求超过500亿美元估值。",
                                              "content": "财联社9月19日电，消息人士称，西屋电气计划在美国IPO中寻求超过500亿美元估值。",
                                              "ctime": 1789745925,
                                              "img": "",
                                              "images": [],
                                              "subjects": [{"subject_name": "环球市场情报"}]
                                            }
                                            """;

    private static string BuildListJson(params string[] items)
    {
        // 手拼而非内插原始字符串 : 结尾的 ]}} 会与 $$""" 的转义规则冲突
        return "{\"errno\":0,\"msg\":\"\",\"data\":{\"roll_data\":[" + string.Join(",", items) + "]}}";
    }

    [TestMethod]
    public void SignMatchesVerifiedVector()
    {
        var parameters = new Dictionary<string, string>
        {
            ["app"] = "CailianpressWeb",
            ["os"] = "web",
            ["sv"] = "8.7.9",
            ["refresh_type"] = "1",
            ["rn"] = "20",
            ["last_time"] = "0"
        };

        var queryString = ClsSignature.BuildQueryString(parameters);

        Assert.AreEqual(VerifiedQueryString, queryString);
        Assert.AreEqual(VerifiedSign, ClsSignature.Sign(queryString));
    }

    [TestMethod]
    public void BuildQueryStringSortsKeysAndDropsEmptyValues()
    {
        var parameters = new Dictionary<string, string>
        {
            ["b"] = "2",
            ["a"] = "1",
            ["empty"] = ""
        };

        Assert.AreEqual("a=1&b=2", ClsSignature.BuildQueryString(parameters));
    }

    [TestMethod]
    public void ParseListPageMapsItemFields()
    {
        var listPage = ClsNewsSpider.ParseListPage(BuildListJson(TitleItemJson), 20);

        Assert.AreEqual(1, listPage.Items.Count);
        var item = listPage.Items[0];
        Assert.AreEqual((int)FromTypeOfNews.ClsMedia, item.FromMedia);
        Assert.AreEqual("https://www.cls.cn/detail/2487643", item.NewsUrl);
        Assert.AreEqual("国际原子能机构新增11个理事会成员国", item.NewsTitle);
        Assert.AreEqual("财联社", item.NewsFrom);
        Assert.AreEqual(ClsNewsResource.TelegraphCategoryNumber, item.Category);
        Assert.IsTrue(item.NewsSummary!.StartsWith("【国际原子能机构"));
        // ctime 1789752848 = 北京时间 2026-09-19 01:34:08
        Assert.AreEqual(new DateTime(2026, 9, 19, 1, 34, 8), item.NewsTime);
    }

    [TestMethod]
    public void ParseListPageFallbackToBriefWhenTitleEmpty()
    {
        var listPage = ClsNewsSpider.ParseListPage(BuildListJson(UntitledItemJson), 20);

        var item = listPage.Items[0];
        Assert.AreEqual(item.NewsSummary, item.NewsTitle);
        // ctime 1789745925 = 北京时间 2026-09-18 23:38:45
        Assert.AreEqual(new DateTime(2026, 9, 18, 23, 38, 45), item.NewsTime);
    }

    [TestMethod]
    public void ParseListPageTruncateLongBriefForTitle()
    {
        var longBrief = new string('财', 80);
        var itemJson = $$"""{"id": 1, "title": "", "brief": "{{longBrief}}", "content": "正文", "ctime": 1789752848}""";

        var listPage = ClsNewsSpider.ParseListPage(BuildListJson(itemJson), 20);

        Assert.AreEqual(60, listPage.Items[0].NewsTitle!.Length);
    }

    [TestMethod]
    public void ParseListPageCarryInlineOriginForEveryItem()
    {
        var listPage = ClsNewsSpider.ParseListPage(BuildListJson(TitleItemJson, UntitledItemJson), 20);

        Assert.AreEqual(2, listPage.Items.Count);
        Assert.AreEqual(listPage.Items.Count, listPage.InlineOrigins.Count);
        var origin = listPage.InlineOrigins[0];
        Assert.AreEqual("https://www.cls.cn/detail/2487643", origin.NewsUrl);
        Assert.AreEqual(NewsContentOriginType.Json, origin.OriginType);
        Assert.AreEqual(NewsContentOriginStatus.Success, origin.Status);
        Assert.IsTrue(origin.NewsOriginContent.Contains("\"id\":2487643"));
    }

    [TestMethod]
    public void ParseListPageNextCursorIsOldestCtimePlusOne()
    {
        var json = BuildListJson(TitleItemJson, UntitledItemJson);

        // 满页 : 游标取最老一条 ctime + 1 ( 接口严格小于 , 不加 1 会漏掉同一秒的条目 )
        var fullPage = ClsNewsSpider.ParseListPage(json, 2);
        Assert.AreEqual("1789745926", fullPage.NextCursor);

        // 短页即末页
        Assert.IsNull(ClsNewsSpider.ParseListPage(json, 20).NextCursor);
    }

    [TestMethod]
    public void ParseListPageThrowOnErrorErrno()
    {
        Assert.ThrowsExactly<HtmlFormException>(() =>
            ClsNewsSpider.ParseListPage("""{"errno":"10012","msg":"签名错误"}""", 20));
    }

    [TestMethod]
    public void ParseContentMapsTextImagesAndKeyword()
    {
        var spider = new ClsNewsSpider();

        var parseResult = spider.ParseContent(TitleItemJson, "https://www.cls.cn/detail/2487643");

        Assert.AreEqual("https://www.cls.cn/detail/2487643", parseResult.Content.NewsUrl);
        Assert.AreEqual("国际原子能机构新增11个理事会成员国", parseResult.Content.NewsTitle);
        Assert.AreEqual("环球市场情报,美股IPO动态", parseResult.Content.NewsKeyword);
        Assert.IsTrue(parseResult.Content.NewsContentText!.Contains("任期为2026年至2028年"));
        Assert.IsTrue(parseResult.Content.NewsContentJson!.Contains(NewsContentSegment.TextType));
        Assert.IsTrue(parseResult.Content.NewsContentJson.Contains(NewsContentSegment.ImgType));
        // 正文图 + 封面图
        Assert.AreEqual(2, parseResult.Images.Count);
        Assert.AreEqual("https://image.cls.cn/images/20260919/body.png", parseResult.Images[0].ImageResourceUrl);
        Assert.AreEqual("cover.png", parseResult.Images[1].ImageName);
    }

    [TestMethod]
    public void RegistryRegisterClsSource()
    {
        var spider = NewsSpiderRegistry.Get((int)FromTypeOfNews.ClsMedia);

        Assert.IsNotNull(spider);
        Assert.IsInstanceOfType<ClsNewsSpider>(spider);
        Assert.AreEqual(FromTypeOfNews.ClsMedia, spider.FromMedia);
        Assert.AreEqual(1, spider.Columns.Count);
    }

    [TestMethod]
    public void GetContentOriginReportFailedWithoutNetwork()
    {
        var spider = new ClsNewsSpider();
        var newsItem = new KSpider.Model.SpiderNewsListModel { NewsUrl = "https://www.cls.cn/detail/1" };

        var origin = spider.GetContentOrigin(newsItem).GetAwaiter().GetResult();

        Assert.AreEqual(NewsContentOriginStatus.Failed, origin.Status);
        Assert.AreEqual("", origin.NewsOriginContent);
    }
}
