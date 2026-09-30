using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.News.Report.Eastmoney;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     东财研报源 ( 第三管线 ) 解析回归 : 全部基于 TestData 里的真实接口响应 , 离线执行。
///     夹具 : df_report_list_qtype0/1/2.json ( 个股/行业/宏观三类列表 ) +
///     df_report_detail_stock/industry/macro.html ( 三类详情页 , 摘要在 div.ctx-content )。
///     重抓夹具时须同步这里依赖固定值的断言 ( infoCode / 标题 / 条目数 )。
/// </summary>
[TestClass]
public class DfResearchReportSpiderTest
{
    private static string ReadFixture(string fileName)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
    }

    [TestMethod]
    public void ParseStockReportPageMapsFields()
    {
        var page = DfResearchReportSpider.ParseReportPage(ReadFixture("df_report_list_qtype0.json"),
            ResearchReportKind.Stock);

        Assert.AreEqual(39, page.Items.Count);
        Assert.AreEqual(1, page.TotalPage);
        Assert.IsFalse(page.HasNextPage(1), "TotalPage=1 无下一页");
        var first = page.Items[0];
        Assert.AreEqual("AP202609301830020241", first.InfoCode);
        Assert.AreEqual("首次覆盖：电子大宗气体龙头厂商，受益半导体景气周期及国产替代", first.Title);
        Assert.AreEqual((int)FromTypeOfNews.DfMedia, first.FromMedia);
        Assert.AreEqual((short)ResearchReportKind.Stock, first.ReportKind);
        Assert.AreEqual("688548", first.StockCode);
        Assert.AreEqual("广钢气体", first.StockName);
        Assert.AreEqual("万联证券", first.OrgName, "机构取简称 orgSName");
        Assert.AreEqual("增持", first.RatingName);
        Assert.AreEqual("电子化学品Ⅱ", first.IndustryName, "个股研报取细分行业");
        Assert.AreEqual(0.35m, first.EpsThisYear);
        Assert.AreEqual(0.42m, first.EpsNextYear);
        Assert.AreEqual(94.16m, first.PeThisYear);
        Assert.IsNull(first.AimPriceHigh, "样本未给目标价 , 空串归一为 null");
        Assert.IsNull(first.AimPriceLow);
        Assert.AreEqual("夏清莹,陈达", first.Researcher);
        Assert.AreEqual(new DateTime(2026, 9, 30), first.PublishDate, "发布时间只到日期级");
        Assert.IsFalse(string.IsNullOrEmpty(first.RawContent), "原始条目 JSON 应保留");
    }

    [TestMethod]
    public void ParseIndustryReportPageHasNoStockDimension()
    {
        var page = DfResearchReportSpider.ParseReportPage(ReadFixture("df_report_list_qtype1.json"),
            ResearchReportKind.Industry);

        // 行业 135 条命中分 2 页 , 首页满 100 条
        Assert.AreEqual(100, page.Items.Count);
        Assert.AreEqual(2, page.TotalPage);
        Assert.IsTrue(page.HasNextPage(1), "TotalPage=2 应翻下一页 ( 停机回补翻页路径 )");
        var first = page.Items[0];
        Assert.AreEqual("AP202609301830023938", first.InfoCode);
        Assert.IsNull(first.StockCode, "行业研报无个股维度");
        Assert.IsNull(first.StockName);
        Assert.AreEqual("国信证券", first.OrgName);
        Assert.AreEqual("游戏Ⅱ", first.IndustryName, "行业研报取所属行业");
        Assert.IsNull(first.EpsThisYear, "行业研报无盈利预测");
    }

    [TestMethod]
    public void ParseMacroReportPageLeavesRatingEmpty()
    {
        var page = DfResearchReportSpider.ParseReportPage(ReadFixture("df_report_list_qtype2.json"),
            ResearchReportKind.Macro);

        Assert.AreEqual(74, page.Items.Count);
        var first = page.Items[0];
        Assert.AreEqual("AP202609301830024638", first.InfoCode);
        Assert.IsNull(first.RatingName, "宏观研报无评级");
        Assert.IsNull(first.IndustryName);
        Assert.IsNull(first.StockCode);
    }

    [TestMethod]
    public void ParseReportPageThrowsOnMissingDataArray()
    {
        // data 非数组 ( 如接口改版返回 data:"" ) 显式报错 , 不能当空页吞掉
        Assert.ThrowsExactly<HtmlFormException>(() =>
            DfResearchReportSpider.ParseReportPage("""{"hits":0,"size":0,"data":""}""", ResearchReportKind.Stock));
        Assert.ThrowsExactly<HtmlFormException>(() =>
            DfResearchReportSpider.ParseReportPage("not json", ResearchReportKind.Stock));
    }

    [TestMethod]
    public void ParseReportPageSkipsItemsWithoutKeyFields()
    {
        const string response = """
                                {"hits":2,"size":2,"TotalPage":1,"data":[
                                  {"infoCode":"","title":"缺 infoCode 跳过","publishDate":"2026-09-30 00:00:00.000"},
                                  {"infoCode":"AP202609309999999999","title":"缺日期跳过"},
                                  {"infoCode":"AP202609309888888888","title":"有效条目","publishDate":"2026-09-30 00:00:00.000",
                                   "stockCode":"","orgSName":"","emRatingName":""}
                                ]}
                                """;

        var page = DfResearchReportSpider.ParseReportPage(response, ResearchReportKind.Stock);

        Assert.AreEqual(1, page.Items.Count, "infoCode / publishDate 缺失的坏数据应跳过");
        Assert.AreEqual("AP202609309888888888", page.Items[0].InfoCode);
        Assert.IsNull(page.Items[0].OrgName, "空串字段归一为 null");
    }

    [TestMethod]
    public void ParseSummaryExtractsParagraphs()
    {
        var summary = DfResearchReportSpider.ParseReportSummary(ReadFixture("df_report_detail_stock.html"));

        // 摘要按段落换行 , 首段是个股标识行
        var lines = summary.Split('\n');
        Assert.IsTrue(lines.Length > 3, $"摘要应有多个段落 , 实际 {lines.Length}");
        Assert.AreEqual("广钢气体(688548)", lines[0], "段落已去全角空格缩进与内嵌标签");
        Assert.IsTrue(summary.Contains("报告关键要素"), "正文段落完整保留");
        Assert.IsFalse(summary.Contains('<'), "不应残留 HTML 标签");
        Assert.IsFalse(summary.Contains("　"), "全角缩进应被清理");
    }

    [TestMethod]
    public void ParseSummaryWorksOnIndustryAndMacroPages()
    {
        var industry = DfResearchReportSpider.ParseReportSummary(ReadFixture("df_report_detail_industry.html"));
        var macro = DfResearchReportSpider.ParseReportSummary(ReadFixture("df_report_detail_macro.html"));

        Assert.IsFalse(string.IsNullOrWhiteSpace(industry), "行业研报详情应有摘要");
        Assert.IsFalse(string.IsNullOrWhiteSpace(macro), "宏观研报详情应有摘要");
    }

    [TestMethod]
    public void ParseSummaryThrowsOnMissingRegion()
    {
        Assert.ThrowsExactly<HtmlFormException>(() =>
            DfResearchReportSpider.ParseReportSummary("<html><body>其它页面</body></html>"));
    }

    [TestMethod]
    public void DetailUrlTemplateMatchesKind()
    {
        // 三类模板路径不同 , 反射私有方法不便 , 这里直接锁定 Resource 常量形态
        Assert.IsTrue(DfResearchReportResource.StockDetailUrlTemplate.Contains("zw_stock.jshtml"));
        Assert.IsTrue(DfResearchReportResource.IndustryDetailUrlTemplate.Contains("zw_industry.jshtml"));
        Assert.IsTrue(DfResearchReportResource.MacroDetailUrlTemplate.Contains("zw_macresearch.jshtml"));
    }
}
