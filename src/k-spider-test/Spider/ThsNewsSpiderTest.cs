using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.News.Flash.Ths;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     同花顺 7x24 快讯源解析回归 : 全部基于 TestData 里的真实接口响应 , 离线执行。
///     夹具 : ths_flash_page1/page2.json ( 全量流两页各 50 条 , 页间零重叠 )。
///     重抓夹具时须同步这里依赖固定值的断言 ( seq / 标题 / 条数 )。
/// </summary>
[TestClass]
public class ThsNewsSpiderTest
{
    private static string ReadFixture(string fileName)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
    }

    [TestMethod]
    public void ParseFlashPageMapsFields()
    {
        var page = ThsNewsSpider.ParseFlashPage(ReadFixture("ths_flash_page1.json"), 50, 1);

        Assert.AreEqual(50, page.Items.Count);
        Assert.AreEqual("2", page.NextCursor, "满页应翻下一页 ( 页码游标 )");
        var first = page.Items[0];
        Assert.AreEqual((int)FromTypeOfNews.ThsMedia, first.FromMedia);
        Assert.AreEqual(501, first.Category);
        Assert.AreEqual("https://news.10jqka.com.cn/20260930/c680400955.shtml", first.NewsUrl);
        Assert.AreEqual("硅料硅片板块走高，弘元绿能涨停", first.Title);
        Assert.IsTrue(first.Content!.StartsWith("硅料硅片板块走高"), "digest 即全文");
        Assert.AreEqual(new DateTime(2026, 9, 30, 13, 39, 10), first.NewsTime, "ctime 按东八区换算");
        Assert.AreEqual((short)2, first.Level, "import=3 红标映射重要 ( level 2 )");
        Assert.AreEqual("异动,A股", first.Keyword, "tag 逗号分隔直接入 keyword");
        Assert.IsNotNull(first.StockList, "关联标的应落 stock_list");
        Assert.IsTrue(first.StockList!.Contains("603185"), "标的代码应保留");
        Assert.IsTrue(first.StockList!.Contains("弘元绿能"), "标的名称应保留");
        Assert.IsNull(first.ImageUrls, "无图条目 image_urls 为空");
        Assert.IsFalse(string.IsNullOrEmpty(first.RawContent), "原始条目 JSON 应保留");
    }

    [TestMethod]
    public void ParseFlashPageMapsNormalImportLevel()
    {
        var page = ThsNewsSpider.ParseFlashPage(ReadFixture("ths_flash_page1.json"), 50, 1);

        // 夹具实测 50 条里 13 条红标 ( level 2 ) 、37 条普通 ( level 1 )
        Assert.AreEqual(13, page.Items.Count(item => item.Level == 2), "红标条目应映射 level 2");
        Assert.AreEqual(37, page.Items.Count(item => item.Level == 1), "普通条目应映射 level 1");

        var plain = page.Items.Single(item => item.NewsUrl.EndsWith("c680400554.shtml"));
        Assert.AreEqual((short)1, plain.Level);
        Assert.IsNull(plain.StockList, "无标的条目 stock_list 为空");
    }

    [TestMethod]
    public void ParseFlashPageStopsOnShortPage()
    {
        // 请求 100 实际钳到 50? 不——本用例用小于实际条数的页长验证短页即末页
        var page = ThsNewsSpider.ParseFlashPage(ReadFixture("ths_flash_page1.json"), 51, 3);

        Assert.IsNull(page.NextCursor, "实得 50 < 请求 51 , 短页即末页");
    }

    [TestMethod]
    public void ParseFlashPageThrowsOnErrorAndMissingList()
    {
        Assert.ThrowsExactly<HtmlFormException>(() =>
            ThsNewsSpider.ParseFlashPage("""{"code":"400","msg":"请求参数错误","data":null}""", 50, 1));
        Assert.ThrowsExactly<HtmlFormException>(() =>
            ThsNewsSpider.ParseFlashPage("""{"code":"200","msg":"","data":{}}""", 50, 1));
    }

    [TestMethod]
    public void ParseFlashPageSkipsBrokenItems()
    {
        const string response = """
                                {"code":"200","msg":"","data":{"list":[
                                  {"seq":"0","title":"缺 seq","digest":"正文","ctime":1790746750,"url":"https://news.10jqka.com.cn/x.shtml"},
                                  {"seq":"100","title":"缺正文","digest":"","ctime":1790746751,"url":"https://news.10jqka.com.cn/y.shtml"},
                                  {"seq":"101","title":"正常","digest":"正文内容","ctime":1790746752,"url":"https://news.10jqka.com.cn/z.shtml","import":"0","tag":""}
                                ]}}
                                """;

        var page = ThsNewsSpider.ParseFlashPage(response, 50, 1);

        Assert.AreEqual(1, page.Items.Count, "缺 seq / 缺正文的坏数据应跳过");
        Assert.IsNull(page.Items[0].Keyword, "空 tag 归一为 null");
    }
}
