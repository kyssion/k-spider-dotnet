using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.Dataset.LootDrop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     Loot Drop 数据集源 ( 第七管线 ) 解析回归 : 全部基于 TestData 里的真实 REST 响应 , 离线执行。
///     夹具 : lootdrop_startups_page1.json ( startups 表前 3 行全字段 ) +
///     lootdrop_startup_edge.json ( 边界行 : status 带脏尾巴 \r\n )。
///     重抓夹具时须同步这里依赖固定值的断言 ( id / 名称 / 融资额 / 条目数 )。
/// </summary>
[TestClass]
public class LootDropDatasetSpiderTest
{
    private static string ReadFixture(string fileName)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
    }

    [TestMethod]
    public void ParseStartupPageMapsFields()
    {
        var rows = LootDropDatasetSpider.ParseStartups(ReadFixture("lootdrop_startups_page1.json"));

        Assert.AreEqual(3, rows.Count);
        var first = rows[0];
        Assert.AreEqual(1L, first.Id, "主键直接取源侧 id");
        Assert.AreEqual("99dresses", first.Name);
        Assert.AreEqual((int)FromTypeOfNews.LootDropMedia, first.FromMedia);
        Assert.AreEqual("Consumer", first.Sector);
        Assert.AreEqual("Australia", first.Country);
        Assert.AreEqual(2010, first.StartYear);
        Assert.AreEqual(2013, first.EndYear);
        Assert.AreEqual(1200000L, first.TotalFunding);
        Assert.AreEqual(3, first.Difficulty);
        Assert.AreEqual(3, first.Scalability);
        Assert.AreEqual("medium", first.MarketPotential);
        Assert.AreEqual("closed", first.Status);
        Assert.AreEqual("Competition", first.PrimaryCauseOfDeath);
        Assert.AreEqual("Marketplace", first.ProductType);
        Assert.AreEqual(148, first.Views);
        Assert.AreEqual(@"[""Nikki Durkin""]", first.Founders, "数组列序列化为 JSON 文本");
        Assert.IsTrue(first.Investors!.Contains("Y Combinator"));
        Assert.IsTrue(first.TheLoot!.StartsWith("["), "教训清单保持 JSON 数组形态");
        Assert.IsTrue(first.PivotIdea!.Contains("LoopWardrobe"), "重建构想保持 JSON 对象形态");
        Assert.IsFalse(string.IsNullOrWhiteSpace(first.Description), "案例导语是完整段落");
        Assert.IsFalse(string.IsNullOrWhiteSpace(first.CauseOfDeath), "死因分析是完整段落");
        Assert.IsFalse(string.IsNullOrWhiteSpace(first.RawContent), "原始条目 JSON 应保留");
        Assert.AreEqual(new DateTime(2026, 1, 26, 21, 7, 20, 221), first.SourceCreatedAt,
            "源侧 ISO 时间转 UTC 去时区 ( 保留毫秒 )");
        Assert.AreEqual(64, first.ContentHash.Length, "内容指纹为 SHA256 十六进制");
        Assert.IsTrue(first.ContentHash.All(char.IsAsciiHexDigit), "指纹应为十六进制字符");
    }

    [TestMethod]
    public void ParseEdgeRowCleansDirtyStrings()
    {
        var rows = LootDropDatasetSpider.ParseStartups(ReadFixture("lootdrop_startup_edge.json"));

        Assert.AreEqual(1, rows.Count);
        var row = rows[0];
        Assert.AreEqual(1126L, row.Id);
        Assert.AreEqual("20n", row.Name);
        Assert.AreEqual("closed", row.Status, "源侧脏尾巴 \\r\\n 应被 Trim");
        Assert.IsTrue(row.Founders!.StartsWith("["), "数组列原样序列化");
    }

    [TestMethod]
    public void ContentHashIsStableAndSensitive()
    {
        var rows = LootDropDatasetSpider.ParseStartups(ReadFixture("lootdrop_startups_page1.json"));

        // 稳定性 : 同内容重复解析 , 指纹一致 ( 全量重拉不产生空转 UPDATE 的前提 )
        var reparsed = LootDropDatasetSpider.ParseStartups(ReadFixture("lootdrop_startups_page1.json"));
        for (var i = 0; i < rows.Count; i++)
            Assert.AreEqual(rows[i].ContentHash, reparsed[i].ContentHash, $"第 {i} 行同内容指纹应一致");

        // 敏感性 : 任一业务字段变化 ( 如源站浏览数增长 ) 指纹必须变
        rows[0].Views = (rows[0].Views ?? 0) + 1;
        var mutatedHash = LootDropDatasetRow.ComputeContentHash(rows[0]);
        Assert.AreNotEqual(reparsed[0].ContentHash, mutatedHash, "views 变化应改变指纹");

        // 管理列不参与 : from_media / raw_content 变化不影响指纹
        rows[0].Views = reparsed[0].Views;
        rows[0].FromMedia = 999;
        rows[0].RawContent = "{}";
        Assert.AreEqual(reparsed[0].ContentHash, LootDropDatasetRow.ComputeContentHash(rows[0]),
            "from_media / raw_content 不参与指纹");
    }

    [TestMethod]
    public void ParseContentRangeTotalReadsPostgrestHeader()
    {
        Assert.AreEqual(1749L, LootDropDatasetSpider.ParseContentRangeTotal("0-0/1749"));
        Assert.AreEqual(1L, LootDropDatasetSpider.ParseContentRangeTotal("0-0/1"));
        Assert.AreEqual(-1L, LootDropDatasetSpider.ParseContentRangeTotal(""), "空串报 -1");
        Assert.AreEqual(-1L, LootDropDatasetSpider.ParseContentRangeTotal("0-0/"), "无总数段报 -1");
    }

    [TestMethod]
    public void ParseNonArrayResponseFailsExplicitly()
    {
        // PostgREST 错误响应是对象 ( 如 {"code":"42P01"...} ) , 必须显式报错而不是当空页
        Assert.ThrowsExactly<KSpider.Exceptions.HtmlFormException>(
            () => LootDropDatasetSpider.ParseStartups("{\"message\":\"JWT invalid\"}"));
        Assert.ThrowsExactly<KSpider.Exceptions.HtmlFormException>(
            () => LootDropDatasetSpider.ParseStartups("not json at all"));
    }
}
