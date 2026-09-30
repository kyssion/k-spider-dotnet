using System.Text.Json.Nodes;
using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.News;
using KSpider.Spider.News.Web.Cls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     财联社文章频道 ( 网页型管线 ) 解析回归 : 全部基于 TestData 里的真实接口响应 , 离线执行。
///     夹具 : cls_depth_list_1000_page1/2.json ( 频道列表两页 ) + cls_article_detail.html ( 详情 SSR 页面 )。
///     重抓夹具时须同步这里依赖固定值的断言 ( 标题 / ctime / 游标 )。
/// </summary>
[TestClass]
public class ClsArticleSpiderTest
{
    private const string DetailUrl = "https://www.cls.cn/detail/2492814";

    private static string ReadFixture(string fileName)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
    }

    /// <summary>
    ///     手工构造列表响应 , 用于广告 / 站外跳转条目的过滤分支 ( 真实夹具里恰好没有这两类条目 )
    /// </summary>
    private static string BuildListJson(params string[] items)
    {
        return "{\"errno\":0,\"msg\":\"\",\"data\":[" + string.Join(",", items) + "]}";
    }

    [TestMethod]
    public void ParseListPageMapsFieldsAndCategory()
    {
        var page = ClsArticleSpider.ParseListPage(ReadFixture("cls_depth_list_1000_page1.json"), 102);

        Assert.IsTrue(page.Items.Count > 0, "频道列表未解析出任何条目");
        var first = page.Items[0];
        Assert.AreEqual("https://www.cls.cn/detail/2492814", first.NewsUrl);
        Assert.AreEqual("谷歌TPU，下周出发去太空", first.NewsTitle);
        Assert.AreEqual("史正丞", first.NewsFrom);
        Assert.AreEqual((int)FromTypeOfNews.ClsMedia, first.FromMedia);
        Assert.AreEqual(102, first.Category);
        Assert.AreEqual(new DateTime(2026, 9, 25, 2, 34, 48), first.NewsTime);
    }

    [TestMethod]
    public void ParseListPageSkipsAdsAndExternalLinks()
    {
        var normal = new JsonObject
        {
            ["id"] = 100,
            ["ctime"] = 1790274888,
            ["title"] = "正常文章",
            ["brief"] = "摘要",
            ["source"] = "财联社",
            ["is_ad"] = 0,
            ["external_link"] = ""
        };
        var ad = new JsonObject
        {
            ["id"] = 101,
            ["ctime"] = 1790274800,
            ["title"] = "广告条目",
            ["is_ad"] = 1,
            ["external_link"] = ""
        };
        var external = new JsonObject
        {
            ["id"] = 102,
            ["ctime"] = 1790274700,
            ["title"] = "站外跳转",
            ["is_ad"] = 0,
            ["external_link"] = "https://example.com/article"
        };

        var page = ClsArticleSpider.ParseListPage(BuildListJson(normal.ToJsonString(), ad.ToJsonString(), external.ToJsonString()), 102);

        Assert.AreEqual(1, page.Items.Count, "广告与站外跳转条目应被跳过");
        Assert.AreEqual("https://www.cls.cn/detail/100", page.Items[0].NewsUrl);
    }

    [TestMethod]
    public void ParseListPageCursorUsesOldestCtimeUntilEmptyPage()
    {
        // 列表按 SortScore 编辑混排 : 服务端不按 rn 裁页 ( 短页判断不可用 , 空页才是末页 ) ;
        // 游标取本页最老 ctime 作"从这里继续"的记号 —— 混排下游标不保证单调向早 ,
        // 两页重叠与新行交错 ( 实测 30 行里 13 行与上一页重叠 ) , 全部交给入库去重吸收 ,
        // 任务侧另有"单页全部已存在即停"与最多 4 页的硬上限兜底
        var page1 = ClsArticleSpider.ParseListPage(ReadFixture("cls_depth_list_1000_page1.json"), 102);
        var page2 = ClsArticleSpider.ParseListPage(ReadFixture("cls_depth_list_1000_page2.json"), 102);

        Assert.AreEqual("1790215690", page1.NextCursor, "游标应为 page1 全页最小 ctime");
        Assert.IsNotNull(page2.NextCursor, "第二页非空 , 仍应给出续拉游标");
        Assert.IsTrue(long.TryParse(page2.NextCursor, out _), "游标应为合法 ctime");
        Assert.AreEqual(30, page2.Items.Count);
    }

    [TestMethod]
    public void ExtractNextDataFromRealDetailPage()
    {
        var json = ClsArticleSpider.ExtractNextDataJson(ReadFixture("cls_article_detail.html"));

        var article = JsonNode.Parse(json)!["props"]!["pageProps"]!["articleDetail"];
        Assert.IsNotNull(article, "articleDetail 应可从 __NEXT_DATA__ 中取到");
        Assert.AreEqual("谷歌TPU，下周出发去太空", article["title"]!.ToString());
    }

    [TestMethod]
    public void ParseContentMapsDetailFieldsSegmentsAndImages()
    {
        var spider = new ClsArticleSpider();
        var originJson = ClsArticleSpider.ExtractNextDataJson(ReadFixture("cls_article_detail.html"));

        var result = spider.ParseContent(originJson, DetailUrl);

        var content = result.Content;
        Assert.AreEqual("谷歌TPU，下周出发去太空", content.NewsTitle);
        Assert.AreEqual("财联社 史正丞", content.NewsFrom);
        Assert.IsTrue(!string.IsNullOrEmpty(content.NewsSummary), "摘要 ( brief ) 不应为空");
        Assert.AreEqual(new DateTime(2026, 9, 25, 2, 34, 48), content.NewsTime);
        Assert.AreEqual("原创", content.NewsKeyword);
        Assert.IsTrue(!string.IsNullOrEmpty(content.NewsContentText), "纯文本聚合不应为空");
        Assert.IsTrue(content.NewsContentText!.Contains("云计算和AI芯片大厂谷歌"), "纯文本应包含正文内容");

        Assert.IsNotNull(content.NewsContentJson);
        Assert.IsTrue(content.NewsContentJson!.Contains(NewsContentSegment.ImgType), "片段中应包含图片");
        Assert.IsTrue(result.Images.Count > 0, "图片列表不应为空");
        Assert.IsTrue(result.Images.All(image => image.NewsUrl == DetailUrl));
        Assert.IsTrue(result.Images.Any(image => image.ImageResourceUrl!.Contains("image.cls.cn")));
    }

    [TestMethod]
    public void ParseContentKeepsBlockquoteAndLinkText()
    {
        // 真实夹具 ( id=2492703 ) 正文含 blockquote 引用块 : 引用是内容的一部分 ,
        // 不能按"未知标签"丢弃 ( 批量实测 20 篇里 blockquote 与顶层 a 各有出现 )
        var spider = new ClsArticleSpider();
        var originJson = ClsArticleSpider.ExtractNextDataJson(ReadFixture("cls_article_detail_rich.html"));

        var result = spider.ParseContent(originJson, "https://www.cls.cn/detail/2492703");

        Assert.AreEqual("甲骨文重磅项目现风险信号：据称正为数据中心延期留后路", result.Content.NewsTitle);
        Assert.IsTrue(result.Content.NewsContentText!.Contains("不可抗力"), "引用块文本应进入纯文本聚合");
        Assert.IsTrue(result.Content.NewsContentJson!.Contains("blockquote"), "片段 TagType 应保留引用块溯源标记");
    }

    [TestMethod]
    public void ParseContentThrowsOnBrokenOrigin()
    {
        var spider = new ClsArticleSpider();
        Assert.ThrowsExactly<DownloadHttpRequestException>(() =>
            spider.ParseContent("{\"props\":{\"pageProps\":{}}}", DetailUrl));
        Assert.ThrowsExactly<HtmlFormException>(() => spider.ParseContent("not-json", DetailUrl));
    }

    // ── 品见 ( 第二列表族 : /v5/web/pinjian/assembled2 拼装流 ) ──

    [TestMethod]
    public void ParsePinjianPageMapsFieldsAndSkipsSubjectCards()
    {
        var page = ClsArticleSpider.ParsePinjianPage(ReadFixture("cls_pinjian_assembled_page1.json"),
            ClsArticleResource.PinjianCategoryNumber);

        // 夹具实测 : 5 个专题 , 11 个置顶专题卡 ( ctype=1 , 跳过 ) , 24 篇真实文章
        Assert.AreEqual(24, page.Items.Count);
        Assert.IsNull(page.NextCursor, "拼装流整页即全量 , 无翻页游标");
        var first = page.Items[0];
        Assert.AreEqual("https://www.cls.cn/detail/2482499", first.NewsUrl, "文章 id 与财联社全局 id 同空间 , 详情页同形态");
        Assert.AreEqual("潮讯 | 世界级酒吧齐聚SIP鸡尾酒节，戴森新一代科技亮相，阿联酋航空推出全球首款电动豪华经济舱座椅，宜家推出厨房局部焕新解决方案", first.NewsTitle);
        Assert.AreEqual("责编：若瑜", first.NewsFrom, "品见署名在 author 字段");
        Assert.AreEqual(ClsArticleResource.PinjianCategoryNumber, first.Category);
        Assert.AreEqual((int)FromTypeOfNews.ClsMedia, first.FromMedia);
        Assert.AreEqual(new DateTime(2026, 9, 14, 17, 41, 31), first.NewsTime, "ctime 按东八区换算");
        Assert.IsFalse(string.IsNullOrEmpty(first.NewsSummary), "brief 应进摘要");
        Assert.IsTrue(page.Items.All(item => item.Category == ClsArticleResource.PinjianCategoryNumber));
    }

    [TestMethod]
    public void ParsePinjianPageThrowsOnMissingArticleArray()
    {
        Assert.ThrowsExactly<HtmlFormException>(() =>
            ClsArticleSpider.ParsePinjianPage("{\"errno\":0,\"data\":{\"banner\":[]}}",
                ClsArticleResource.PinjianCategoryNumber));
        Assert.ThrowsExactly<HtmlFormException>(() =>
            ClsArticleSpider.ParsePinjianPage("{\"errno\":10012,\"msg\":\"签名错误\"}",
                ClsArticleResource.PinjianCategoryNumber));
    }

    [TestMethod]
    public void ParsePinjianPageSkipsAdAndExternalItems()
    {
        // 用真实结构构造的广告与站外条目 ( 夹具里 24 篇全是正常条目 , 分支用构造样本锁定 )
        const string response = """
                                {"errno":0,"msg":"","data":{"banner":[],"pinjian_home_article":[
                                  {"subject_id":8421,"pinjian_article_arr":[
                                    {"id":2482500,"title":"广告条目","ctime":1789378891,"ctype":0,"is_ad":1,"external_link":"","brief":"","author":""},
                                    {"id":2482501,"title":"站外条目","ctime":1789378892,"ctype":0,"is_ad":0,"external_link":"https://example.com","brief":"","author":""},
                                    {"id":0,"title":"缺 id","ctime":1789378893,"ctype":0,"is_ad":0,"external_link":"","brief":"","author":""},
                                    {"id":2482502,"title":"正常条目","ctime":1789378894,"ctype":0,"is_ad":0,"external_link":"","brief":"摘要","author":"责编：某人"}
                                  ]}
                                ]}}
                                """;

        var page = ClsArticleSpider.ParsePinjianPage(response, ClsArticleResource.PinjianCategoryNumber);

        Assert.AreEqual(1, page.Items.Count, "广告 / 站外 / 缺 id 条目应跳过");
        Assert.AreEqual("https://www.cls.cn/detail/2482502", page.Items[0].NewsUrl);
        Assert.AreEqual("责编：某人", page.Items[0].NewsFrom);
    }

    [TestMethod]
    public void PinjianColumnIsExposed()
    {
        var spider = new ClsArticleSpider();
        var pinjian = spider.Columns.Single(column => column.ColumnId == ClsArticleResource.PinjianColumnId);
        Assert.AreEqual(ClsArticleResource.PinjianChannelName, pinjian.ColumnName);
        // depth 频道 13 个 + 品见 1 个
        Assert.AreEqual(14, spider.Columns.Count);
    }
}
