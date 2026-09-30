using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.Policy;
using KSpider.Spider.Policy.Csrc;
using KSpider.Spider.Policy.Stats;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     官方文件源 ( Policy 管线 ) 解析回归 : 全部基于 TestData 里的真实页面 , 离线执行。
///     夹具 : stats_policy_list/detail.html ( 统计局「最新发布」列表 + PMI 详情 ,
///     详情原样 UTF-8 ) + csrc_policy_list/detail.html ( 证监会「要闻」全量列表 + 详情 )。
///     重抓夹具时须同步这里依赖固定值的断言 ( 条数 / 标题 / 时间 )。
/// </summary>
[TestClass]
public class PolicySpiderTest
{
    private static string ReadFixture(string fileName)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
    }

    // ── 统计局 ──

    [TestMethod]
    public void StatsParseListPageMapsFieldsAndDedups()
    {
        var page = StatsPolicySpider.ParseListPage(ReadFixture("stats_policy_list.html"));

        // 列表 45 个锚点 ( 轮播与列表区重复 ) → doc_id 去重后 24 条
        Assert.IsTrue(page.Items.Count is >= 10 and <= 25, $"条数异常 : {page.Items.Count}");
        Assert.IsFalse(page.HasMore, "统计局列表整页即全量");
        // 不依赖文档顺序 ( 轮播/推荐区可能置前旧文 ) , 按 doc_id 定位
        var pmi = page.Items.First(item => item.DocId == "202609/t20260930_1965449");
        Assert.AreEqual("2026年9月中国采购经理指数运行情况", pmi.Title);
        Assert.AreEqual("https://www.stats.gov.cn/sj/zxfb/202609/t20260930_1965449.html", pmi.Url);
        Assert.AreEqual(new DateTime(2026, 9, 30), pmi.PublishTime, "列表时间取 doc_id 日期当日 00:00");
        Assert.AreEqual(page.Items.Count, page.Items.Select(item => item.DocId).Distinct().Count(), "doc_id 应已去重");
    }

    [TestMethod]
    public void StatsParseListPageSkipsForeignLinks()
    {
        const string html = """
                            <html><body>
                            <a href="./202609/t20260930_1965449.html">2026年9月中国采购经理指数运行情况</a>
                            <a href="https://www.stats.gov.cn/sj/zxfb/">栏目链接 ( 非文档 )</a>
                            <a href="./other/page.html">非文档形态</a>
                            </body></html>
                            """;

        var page = StatsPolicySpider.ParseListPage(html);

        Assert.AreEqual(1, page.Items.Count, "栏目链接与非文档形态应被排除");
    }

    [TestMethod]
    public void StatsParseDocumentExtractsContentAndAttachments()
    {
        var stub = new PolicyDocumentStub("202609/t20260930_1965449",
            "2026年9月中国采购经理指数运行情况",
            "https://www.stats.gov.cn/sj/zxfb/202609/t20260930_1965449.html",
            new DateTime(2026, 9, 30));

        var doc = StatsPolicySpider.ParseDocument(ReadFixture("stats_policy_detail.html"), stub,
            FromTypeOfNews.StatsMedia, "国家统计局");

        Assert.AreEqual((int)FromTypeOfNews.StatsMedia, doc.FromMedia);
        Assert.AreEqual("国家统计局", doc.Institution);
        Assert.AreEqual(new DateTime(2026, 9, 30, 9, 30, 0), doc.PublishTime, "详情时间 yyyy/MM/dd HH:mm");
        Assert.IsFalse(string.IsNullOrWhiteSpace(doc.ContentText), "正文纯文本不应为空");
        Assert.IsTrue(doc.ContentText!.Contains("采购经理指数"), "正文应含标题主题");
        // 详情页含 8 个数据表格 ( PMI 数据 ) , 表格行应文本化进正文
        Assert.IsTrue(doc.ContentText!.Contains("|") || doc.ContentText!.Length > 500,
            "正文应包含表格文本化内容或足量段落");
        Assert.IsNotNull(doc.Attachments, "PMI 详情带数据表 xls 附件");
        Assert.IsTrue(doc.Attachments!.Contains("https://www.stats.gov.cn/sj/zxfb/202609/P"), "附件应为完整地址");
        Assert.IsFalse(string.IsNullOrEmpty(doc.RawContent));
    }

    [TestMethod]
    public void StatsParseDocumentThrowsOnMissingContainer()
    {
        Assert.ThrowsExactly<HtmlFormException>(() =>
            StatsPolicySpider.ParseDocument("<html><body>其它页面</body></html>",
                new PolicyDocumentStub("202609/t20260930_1", "其它页面",
                    "https://www.stats.gov.cn/sj/zxfb/202609/t20260930_1.html", DateTime.Today),
                FromTypeOfNews.StatsMedia, "国家统计局"));
    }

    // ── 证监会 ──

    [TestMethod]
    public void CsrcParseListPageMapsFields()
    {
        var page = CsrcPolicySpider.ParseListPage(ReadFixture("csrc_policy_list.html"));

                // 列表页聚合多栏目最新内容 , csrc 会管口径条目 14 条 ( 地方局 /jilin/ 等辖区链接被过滤 )
        Assert.IsTrue(page.Items.Count is >= 10 and <= 20, $"条数异常 : {page.Items.Count}");
        Assert.IsFalse(page.HasMore, "全量列表单页 ( common_xq_list 无静态分页 , 其余栏目是独立列表页 )");
        // 不依赖文档顺序 , 按已知条目定位 ( c7657131 = 吴清主席会见 , 2026-09-07 )
        var target = page.Items.First(item => item.DocId == "c7657131");
        Assert.AreEqual("https://www.csrc.gov.cn/csrc/c106311/c7657131/content.shtml", target.Url);
        Assert.IsTrue(target.Title.Contains("穆巴拉克"), $"标题异常 : {target.Title}");
        Assert.AreEqual(new DateTime(2026, 9, 7), target.PublishTime);
        Assert.AreEqual(page.Items.Count, page.Items.Select(item => item.DocId).Distinct().Count());
    }

    [TestMethod]
    public void CsrcParseDocumentExtractsMetaAndContent()
    {
        var stub = new PolicyDocumentStub("c7657131",
            "吴清主席会见沙特投资部助理大臣穆巴拉克",
            "https://www.csrc.gov.cn/csrc/c106311/c7657131/content.shtml",
            new DateTime(2026, 9, 7));

        var doc = CsrcPolicySpider.ParseDocument(ReadFixture("csrc_policy_detail.html"), stub,
            FromTypeOfNews.CsrcMedia, "中国证监会");

        Assert.AreEqual((int)FromTypeOfNews.CsrcMedia, doc.FromMedia);
        Assert.AreEqual("证监会", doc.Institution, "meta ContentSource 优先 ( 官方署名为准 )");
        Assert.AreEqual(new DateTime(2026, 9, 7, 19, 44, 1), doc.PublishTime, "meta PubDate 精确到秒");
        Assert.IsFalse(string.IsNullOrWhiteSpace(doc.ContentText), "正文不应为空");
        Assert.IsTrue(doc.ContentText!.Contains("穆巴拉克"), "正文应含主题 ( font 混排已清洗 )");
        Assert.IsNull(doc.Attachments, "该篇无文档附件");
    }

    [TestMethod]
    public void CsrcParseDocumentThrowsOnMissingContainer()
    {
        Assert.ThrowsExactly<HtmlFormException>(() =>
            CsrcPolicySpider.ParseDocument("<html><body>其它页面</body></html>",
                new PolicyDocumentStub("c1", "其它页面",
                    "https://www.csrc.gov.cn/csrc/c106311/c1/content.shtml", DateTime.Today),
                FromTypeOfNews.CsrcMedia, "中国证监会"));
    }

    // ── 源清单契约 ──

    [TestMethod]
    public void PolicyRegistryContainsTwoSources()
    {
        Assert.AreEqual(2, PolicySpiderRegistry.All.Count);
        Assert.IsTrue(PolicySpiderRegistry.All.All(spider => !string.IsNullOrWhiteSpace(spider.Institution)));
        Assert.IsTrue(PolicySpiderRegistry.All.All(spider => spider.Columns.Count > 0));
    }
}
