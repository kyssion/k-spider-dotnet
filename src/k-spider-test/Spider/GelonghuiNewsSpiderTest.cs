using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.News.Flash.Gelonghui;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     格隆汇 live 快讯源解析回归 : 全部基于 TestData 里的真实接口响应 , 离线执行。
///     夹具 : glh_live_page1/page2.json ( v4 接口固定 15 条 , liveId 翻页页间零重叠 )。
///     重抓夹具时须同步这里依赖固定值的断言 ( id / 标题 / 条数 )。
/// </summary>
[TestClass]
public class GelonghuiNewsSpiderTest
{
    private static string ReadFixture(string fileName)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
    }

    [TestMethod]
    public void ParseFlashPageMapsFields()
    {
        var page = GelonghuiNewsSpider.ParseFlashPage(ReadFixture("glh_live_page1.json"));

        Assert.AreEqual(15, page.Items.Count, "v4 接口固定 15 条");
        Assert.AreEqual("2694746", page.NextCursor, "满页应给下一页 liveId 游标 ( 本页最老 id )");
        var first = page.Items[0];
        Assert.AreEqual((int)FromTypeOfNews.GelonghuiMedia, first.FromMedia);
        Assert.AreEqual(601, first.Category);
        Assert.AreEqual("https://www.gelonghui.com/live/2694771", first.NewsUrl, "route 自带规范详情页地址");
        Assert.AreEqual("AI行情退潮 韩国股市三季度全球主要市场垫底", first.Title);
        Assert.IsTrue(first.Content!.StartsWith("格隆汇9月30日"), "content 全文保留原文前缀");
        Assert.AreEqual(new DateTime(2026, 9, 30, 14, 4, 54), first.NewsTime, "createTimestamp 按东八区换算");
        Assert.AreEqual((short)2, first.Level, "level=1 红标映射重要 ( level 2 )");
        Assert.IsNull(first.Keyword, "v4 条目无标签字段");
    }

    [TestMethod]
    public void ParseFlashPageExtractsStocksAndLevels()
    {
        var page1 = GelonghuiNewsSpider.ParseFlashPage(ReadFixture("glh_live_page1.json"));
        var page2 = GelonghuiNewsSpider.ParseFlashPage(ReadFixture("glh_live_page2.json"));

        // 夹具实测 : 页1 重要 3 条 / 普通 12 条
        Assert.AreEqual(3, page1.Items.Count(item => item.Level == 2));
        Assert.AreEqual(12, page1.Items.Count(item => item.Level == 1));
        // 标的提取 ( 实测 relatedStocks 含 market/code/name )
        var withStock = page1.Items.Concat(page2.Items).FirstOrDefault(item => item.StockList != null);
        if (withStock != null)
        {
            Assert.IsTrue(withStock.StockList!.Contains("stock_id"), "标的统一形态含 stock_id");
            Assert.IsTrue(withStock.StockList!.Contains("name"));
        }

        // 页2 游标连续性 : 满页给游标且比页1 更老
        if (page2.Items.Count >= 15)
            Assert.IsTrue(long.Parse(page2.NextCursor!) < long.Parse(page1.NextCursor!),
                "页2 游标应比页1 更老");
        var overlap = page1.Items.Select(item => item.NewsUrl)
            .Intersect(page2.Items.Select(item => item.NewsUrl)).Count();
        Assert.AreEqual(0, overlap, "liveId 翻页两页不应重叠 ( 实测语义 )");
    }

    [TestMethod]
    public void ParseFlashPageThrowsOnErrorAndMissingResult()
    {
        Assert.ThrowsExactly<HtmlFormException>(() =>
            GelonghuiNewsSpider.ParseFlashPage("""{"statusCode":404,"message":"Not Found"}"""));
        Assert.ThrowsExactly<HtmlFormException>(() =>
            GelonghuiNewsSpider.ParseFlashPage("""{"statusCode":200,"message":"","totalCount":0}"""));
    }

    [TestMethod]
    public void ParseFlashPageSkipsBrokenItems()
    {
        const string response = """
                                {"statusCode":200,"message":"","totalCount":3,"result":[
                                  {"id":0,"title":"缺 id","content":"正文","route":"https://www.gelonghui.com/live/1","createTimestamp":1790748294,"level":0},
                                  {"id":2,"title":"缺正文","content":"","route":"https://www.gelonghui.com/live/2","createTimestamp":1790748295,"level":0},
                                  {"id":3,"title":"缺 route","content":"正文","createTimestamp":1790748296,"level":1},
                                  {"id":4,"title":"","content":"无标题条目用正文截断兜底","route":"https://www.gelonghui.com/live/4","createTimestamp":1790748297,"level":0}
                                ]}
                                """;

        var page = GelonghuiNewsSpider.ParseFlashPage(response);

        Assert.AreEqual(1, page.Items.Count, "缺 id / 缺正文 / 缺 route 的坏数据应跳过");
        Assert.AreEqual("无标题条目用正文截断兜底", page.Items[0].Title, "空标题用正文截断兜底");
        Assert.IsNull(page.NextCursor, "3 条有效 < 15 短页即末页");
    }
}
