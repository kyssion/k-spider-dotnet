using System.Text.Json.Nodes;
using k_spider_dotnet.exception;
using k_spider_dotnet.spider.df_news;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace k_spider_test.spider;

/// <summary>
///     东方财富新闻详情 JSON 解析测试 ( 离线样例数据 , 不访问网络 )
/// </summary>
[TestClass]
public class DfContentSpiderTest
{
    private const string NewsUrl = "https://finance.eastmoney.com/a/202609061234567.html";

    private static string BuildArticleJson(string html)
    {
        var article = new JsonObject
        {
            ["Art_Title"] = "央行宣布降准0.5个百分点",
            ["Art_Media_Name"] = "东方财富网",
            ["Art_ShowTime"] = "2026/09/06 10:30:00",
            ["Art_Keyword"] = "央行,降准",
            ["Art_Content"] = html
        };
        return new JsonObject { ["data"] = article }.ToJsonString();
    }

    [TestMethod]
    public void ParseArticleBaseInfo()
    {
        var spider = new DfContentSpider();
        var info = spider.GetContentInfoByJson(BuildArticleJson("<p>正文第一段。</p>"), NewsUrl);

        Assert.AreEqual("央行宣布降准0.5个百分点", info.NewsTitle);
        // 当前实现摘要直接复用标题
        Assert.AreEqual(info.NewsTitle, info.NewsSummary);
        Assert.AreEqual("东方财富网", info.NewsFrom);
        Assert.AreEqual("2026/09/06 10:30:00", info.NewsTime);
        Assert.AreEqual("央行,降准", info.NewsKeyword);
        Assert.AreEqual(NewsUrl, info.NewsUrl);
    }

    [TestMethod]
    public void ParseParagraphTextIntoContentText()
    {
        var spider = new DfContentSpider();
        var info = spider.GetContentInfoByJson(BuildArticleJson("<p>正文第一段。</p><p>正文第二段。</p>"), NewsUrl);

        Assert.IsTrue(info.NewsDataContentText!.Contains("正文第一段。"));
        Assert.IsTrue(info.NewsDataContentText.Contains("正文第二段。"));
    }

    [TestMethod]
    public void ParseSkipParagraphWithClassAttribute()
    {
        var spider = new DfContentSpider();
        var info = spider.GetContentInfoByJson(
            BuildArticleJson("<p>正文第一段。</p><p class=\"ad\">广告段落</p>"), NewsUrl);

        Assert.IsTrue(info.NewsDataContentText!.Contains("正文第一段。"));
        Assert.IsFalse(info.NewsDataContentText.Contains("广告段落"));
    }

    [TestMethod]
    public void ParseFilterKnownAdText()
    {
        var spider = new DfContentSpider();
        var info = spider.GetContentInfoByJson(
            BuildArticleJson("<p>主力资金加仓名单实时更新</p><p>正文第一段。</p>"), NewsUrl);

        Assert.IsFalse(info.NewsDataContentText!.Contains("主力资金加仓名单实时更新"));
    }

    [TestMethod]
    public void ParseCollectImageFromParagraph()
    {
        var spider = new DfContentSpider();
        var info = spider.GetContentInfoByJson(
            BuildArticleJson("<p><img src=\"https://img.eastmoney.com/demo1.jpg\"/></p>"), NewsUrl);

        Assert.AreEqual(1, info.ImgInfos.Count);
        Assert.AreEqual("https://img.eastmoney.com/demo1.jpg", info.ImgInfos[0].ResourceUrl);
    }

    [TestMethod]
    public void ParseUnorderedListToJsonValue()
    {
        var spider = new DfContentSpider();
        var info = spider.GetContentInfoByJson(
            BuildArticleJson("<ul><li>要点一</li><li>要点二</li></ul>"), NewsUrl);

        var ulDetail = info.NewsDataContent!.Single(item => item.ValueType == "UL");
        Assert.AreEqual("[\"要点一\",\"要点二\"]", ulDetail.Value);
    }

    [TestMethod]
    public void ParseTableToJsonValue()
    {
        var spider = new DfContentSpider();
        var info = spider.GetContentInfoByJson(
            BuildArticleJson("<table><tr><td>单元格A</td><td>单元格B</td></tr></table>"), NewsUrl);

        var tableDetail = info.NewsDataContent!.Single(item => item.ValueType == "TABLE");
        Assert.AreEqual("[[\"单元格A\",\"单元格B\"]]", tableDetail.Value);
    }

    [TestMethod]
    public void ParseThrowWhenDataNodeMissing()
    {
        var spider = new DfContentSpider();
        Assert.ThrowsExactly<DownloadHttpRequestException>(() =>
            spider.GetContentInfoByJson("""{"code":1,"message":"success"}""", NewsUrl));
    }

    [TestMethod]
    public void ParseThrowWhenResponseNotJson()
    {
        var spider = new DfContentSpider();
        Assert.ThrowsExactly<HtmlFormException>(() =>
            spider.GetContentInfoByJson("系统繁忙，请稍后再试。", NewsUrl));
    }
}
