using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.Ranking.Eastmoney;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     东财盘面榜单源 ( Ranking 管线 ) 解析回归 : 全部基于 TestData 里的真实接口响应 , 离线执行。
///     夹具 : df_ranking_lhb.json ( 龙虎榜 ) / df_ranking_blocktrade.json ( 大宗 ) /
///     df_ranking_rzrq.json ( 两融 ) 各 30 条 , 均为 2026-09-29 交易日真实响应。
///     重抓夹具时须同步这里依赖固定值的断言 ( 行数 / 样例代码 )。
/// </summary>
[TestClass]
public class DfRankingSpiderTest
{
    private static string ReadFixture(string fileName)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
    }

    [TestMethod]
    public void ParseLhbPageMapsFieldsAndRowKey()
    {
        var page = DfRankingSpider.ParseReportPage(ReadFixture("df_ranking_lhb.json"), RankingType.Lhb);

        Assert.IsTrue(page.Items.Count is >= 25 and <= 30, $"条数异常 : {page.Items.Count}");
        var first = page.Items[0];
        Assert.AreEqual((int)FromTypeOfNews.DfMedia, first.FromMedia);
        Assert.AreEqual((short)RankingType.Lhb, first.RankingType);
        Assert.AreEqual(new DateTime(2026, 9, 29), first.TradeDate, "交易日期");
        Assert.AreEqual("920229", first.StockCode);
        Assert.IsNotNull(first.RowKey, "行键 = 代码 + 上榜原因");
        Assert.IsTrue(first.RowKey!.StartsWith("920229|"), "行键以代码开头");
        Assert.AreEqual(49.14m, first.ClosePrice);
        Assert.AreEqual(-12.0616m, first.ChangeRate);
        Assert.AreEqual(1271770915.23m, first.DealAmount, "榜内成交额");
        Assert.IsNotNull(first.NetAmount, "榜内净买入");
        Assert.IsNotNull(first.BuyAmount);
        Assert.IsNotNull(first.SellAmount);
        Assert.IsFalse(string.IsNullOrEmpty(first.Detail), "类型长尾进 detail");
        Assert.IsFalse(string.IsNullOrEmpty(first.RawContent));
        // 行键唯一性 : 同代码同原因不重复
        Assert.AreEqual(page.Items.Count, page.Items.Select(item => $"{item.TradeDate:yyyyMMdd}|{item.RowKey}").Distinct().Count(),
            "行键不得重复");
    }

    [TestMethod]
    public void ParseBlockTradePageMapsCounterpart()
    {
        var page = DfRankingSpider.ParseReportPage(ReadFixture("df_ranking_blocktrade.json"), RankingType.BlockTrade);

        Assert.IsTrue(page.Items.Count is >= 25 and <= 30, $"条数异常 : {page.Items.Count}");
        var first = page.Items[0];
        Assert.AreEqual((short)RankingType.BlockTrade, first.RankingType);
        Assert.AreEqual("002110", first.StockCode);
        Assert.IsNull(first.NetAmount, "大宗无净额列");
        Assert.IsNull(first.BuyAmount, "大宗买卖方在 detail 里 , 不占通用列");
        var withCounterpart = page.Items.First(item =>
            item.Detail!.Contains("BUYER_NAME") && !item.Detail.Contains("\"BUYER_NAME\":null"));
        Assert.IsTrue(withCounterpart.Detail!.Contains("SELLER_NAME"), "买卖营业部保留在 detail");
        Assert.AreEqual(page.Items.Count,
            page.Items.Select(item => $"{item.TradeDate:yyyyMMdd}|{item.RowKey}").Distinct().Count(),
            "大宗行键 ( 代码+买卖方+价格 ) 不得重复");
    }

    [TestMethod]
    public void ParseMarginPageUsesCodeAsRowKey()
    {
        var page = DfRankingSpider.ParseReportPage(ReadFixture("df_ranking_rzrq.json"), RankingType.Margin);

        Assert.IsTrue(page.Items.Count is >= 25 and <= 30, $"条数异常 : {page.Items.Count}");
        var first = page.Items[0];
        Assert.AreEqual((short)RankingType.Margin, first.RankingType);
        Assert.AreEqual(first.StockCode, first.RowKey, "两融行键 = 代码");
        Assert.IsNull(first.ClosePrice, "两融无收盘价通用列");
        Assert.IsNull(first.DealAmount, "两融数值全部在 detail ( 余额/买入额等 )");
        Assert.IsTrue(first.Detail!.Contains("RZYE"), "融资余额进 detail");
        Assert.AreEqual(page.Items.Count, page.Items.Select(item => item.RowKey).Distinct().Count(),
            "两融同日行键 ( 代码 ) 不得重复");
    }

    [TestMethod]
    public void ParseReportPageHandlesEmptyWindowAndErrors()
    {
        // 停市窗口 : success=true 但 result=null → 空页 ( 契约 : 当日无披露 )
        var empty = DfRankingSpider.ParseReportPage(
            """{"version":null,"result":null,"success":true,"message":null,"code":0}""", RankingType.Lhb);
        Assert.AreEqual(0, empty.Items.Count);
        Assert.AreEqual(1, empty.TotalPage);

        Assert.ThrowsExactly<HtmlFormException>(() =>
            DfRankingSpider.ParseReportPage(
                """{"version":null,"result":null,"success":false,"message":"报表配置不存在","code":9501}""",
                RankingType.Lhb));
        Assert.ThrowsExactly<HtmlFormException>(() =>
            DfRankingSpider.ParseReportPage("not json", RankingType.Lhb));
    }

    [TestMethod]
    public void ParseReportPageSkipsBrokenItems()
    {
        const string response = """
                                {"version":null,"success":true,"result":{"pages":1,"count":2,"data":[
                                  {"TRADE_DATE":"2026-09-29 00:00:00","SECURITY_CODE":"","SECURITY_NAME_ABBR":"缺代码"},
                                  {"SECURITY_CODE":"000001","SECURITY_NAME_ABBR":"缺日期"},
                                  {"TRADE_DATE":"2026-09-29 00:00:00","SECURITY_CODE":"000002","SECURITY_NAME_ABBR":"万科A",
                                   "CHANGE_TYPE":"日榜","CLOSE_PRICE":"7.10","BILLBOARD_DEAL_AMT":"100000"}
                                ]}}
                                """;

        var page = DfRankingSpider.ParseReportPage(response, RankingType.Lhb);

        Assert.AreEqual(1, page.Items.Count, "缺代码 / 缺日期的坏数据应跳过");
        Assert.AreEqual("000002", page.Items[0].StockCode);
    }
}
