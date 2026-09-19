using KSpider.Exceptions;
using KSpider.Spider.News.Flash.Cls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     财联社电报源测试 : 签名向量 / 快讯解析 ( 全部离线 , 不访问网络 )
/// </summary>
[TestClass]
public class ClsNewsSpiderTest
{
    // 实测向量 : 请求 rn=20 时前端实际使用的 query 与签名 ( 2026-09-19 抓包核对 )
    private const string VerifiedQueryString =
        "app=CailianpressWeb&last_time=0&os=web&refresh_type=1&rn=20&sv=8.7.9";
    private const string VerifiedSign = "e11ef7d616d8f9a2f056e6df1aefc4d4";

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
    public void ParseFlashPageMapCompleteRecord()
    {
        const string itemJson = """
                                {
                                  "id": 2487643,
                                  "title": "国际原子能机构新增11个理事会成员国",
                                  "brief": "【国际原子能机构新增11个理事会成员国】财联社9月19日电。",
                                  "content": "【国际原子能机构新增11个理事会成员国】财联社9月19日电，11个新当选国家将加入。",
                                  "ctime": 1789752848,
                                  "level": "A",
                                  "img": "https://image.cls.cn/images/20260919/cover.png",
                                  "images": ["https://image.cls.cn/images/20260919/body.png"],
                                  "subjects": [{"subject_id": 1556, "subject_name": "环球市场情报"}, {"subject_id": 1557, "subject_name": "美股IPO动态"}],
                                  "stock_list": [{"StockID": "sz300476", "name": "胜宏科技", "RiseRange": 1.86}]
                                }
                                """;

        var page = ClsNewsSpider.ParseFlashPage(BuildListJson(itemJson), 20);

        Assert.AreEqual(1, page.Items.Count);
        var item = page.Items[0];
        Assert.AreEqual(2, item.FromMedia);
        Assert.AreEqual(101, item.Category);
        Assert.AreEqual("https://www.cls.cn/detail/2487643", item.NewsUrl);
        Assert.AreEqual(new DateTime(2026, 9, 19, 1, 34, 8), item.NewsTime);
        Assert.AreEqual("国际原子能机构新增11个理事会成员国", item.Title);
        Assert.IsTrue(item.Content!.Contains("11个新当选国家"));
        Assert.AreEqual("环球市场情报,美股IPO动态", item.Keyword);
        // level A → 3 重大
        Assert.AreEqual(3, item.Level);
        // 关联标的提取为统一形态
        Assert.AreEqual("[{\"stock_id\":\"sz300476\",\"name\":\"胜宏科技\"}]", item.StockList);
        // 正文图 + 封面图
        Assert.AreEqual("[\"https://image.cls.cn/images/20260919/body.png\",\"https://image.cls.cn/images/20260919/cover.png\"]",
            item.ImageUrls);
        Assert.IsTrue(item.RawContent!.Contains("\"id\":2487643"));
    }

    [TestMethod]
    public void ParseFlashPageLevelMapping()
    {
        var bItem = """{"id":1,"title":"t","brief":"b","content":"c","ctime":1789752848,"level":"B"}""";
        var cItem = """{"id":2,"title":"t","brief":"b","content":"c","ctime":1789752848,"level":"C"}""";

        var page = ClsNewsSpider.ParseFlashPage(BuildListJson(bItem, cItem), 20);

        Assert.AreEqual(2, page.Items[1 - 1].Level);
        Assert.AreEqual(1, page.Items[1].Level);
        // 无标的与图片时为 null 而不是空串
        Assert.IsNull(page.Items[0].StockList);
        Assert.IsNull(page.Items[0].ImageUrls);
    }

    [TestMethod]
    public void ParseFlashPageFallbackToBriefWhenTitleEmpty()
    {
        const string itemJson = """
                                {
                                  "id": 2487644,
                                  "title": "",
                                  "brief": "财联社9月19日电，消息人士称，西屋电气计划在美国IPO中寻求超过500亿美元估值。",
                                  "content": "财联社9月19日电，消息人士称，西屋电气计划在美国IPO中寻求超过500亿美元估值。",
                                  "ctime": 1789745925
                                }
                                """;

        var page = ClsNewsSpider.ParseFlashPage(BuildListJson(itemJson), 20);

        var item = page.Items[0];
        Assert.AreEqual(item.Content, item.Title);
        // 无 level 字段默认 C → 1
        Assert.AreEqual(1, item.Level);
    }

    [TestMethod]
    public void ParseFlashPageNextCursorIsOldestCtimePlusOne()
    {
        const string first = """{"id":1,"title":"t","brief":"b","content":"c","ctime":1789752848}""";
        const string second = """{"id":2,"title":"t","brief":"b","content":"c","ctime":1789745925}""";
        var json = BuildListJson(first, second);

        // 满页 : 游标取最老一条 ctime + 1 ( 接口严格小于 , 不加 1 会漏掉同一秒的条目 )
        var fullPage = ClsNewsSpider.ParseFlashPage(json, 2);
        Assert.AreEqual("1789745926", fullPage.NextCursor);

        // 短页即末页
        Assert.IsNull(ClsNewsSpider.ParseFlashPage(json, 20).NextCursor);
    }

    [TestMethod]
    public void ParseFlashPageThrowOnErrorErrno()
    {
        Assert.ThrowsExactly<HtmlFormException>(() =>
            ClsNewsSpider.ParseFlashPage("""{"errno":"10012","msg":"签名错误"}""", 20));
    }
}
