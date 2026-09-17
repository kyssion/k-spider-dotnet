using System.Text.Json.Nodes;
using KSpider.Spider;
using KSpider.Spider.DfNews;
using KSpider.Spider.News;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     多源新闻抽象层测试 : 源注册表与东财源的接口适配 ( 全部离线 , 不访问网络 )
/// </summary>
[TestClass]
public class NewsSpiderTest
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
    public void RegistryRegisterDfSource()
    {
        var spider = NewsSpiderRegistry.Get((int)FromTypeOfNews.DfMedia);

        Assert.IsNotNull(spider);
        Assert.AreEqual(FromTypeOfNews.DfMedia, spider.FromMedia);
        Assert.IsInstanceOfType<DfNewsSpider>(spider);
    }

    [TestMethod]
    public void RegistryReturnNullForUnknownSource()
    {
        Assert.IsNull(NewsSpiderRegistry.Get(999));
    }

    [TestMethod]
    public void DfSpiderColumnsMatchResourceList()
    {
        var spider = new DfNewsSpider();

        Assert.AreEqual(DfNewsResource.DfListUrlResourceList.Length, spider.Columns.Count);
        CollectionAssert.AreEqual(
            DfNewsResource.DfListUrlResourceList.Select(item => item.ListResourceNumber.ToString()).ToList(),
            spider.Columns.Select(item => item.ColumnId).ToList());
        Assert.IsTrue(spider.Columns.All(item => !string.IsNullOrEmpty(item.ColumnName)));
    }

    [TestMethod]
    public void DfSpiderParseContentMapsModelAndImages()
    {
        var spider = new DfNewsSpider();
        var json = BuildArticleJson("<p>正文第一段。</p><p><img src=\"https://img.eastmoney.com/demo1.jpg\"/></p>");

        var result = spider.ParseContent(json, NewsUrl);

        Assert.AreEqual("央行宣布降准0.5个百分点", result.Content.NewsTitle);
        Assert.AreEqual("东方财富网", result.Content.NewsFrom);
        Assert.AreEqual(NewsUrl, result.Content.NewsUrl);
        Assert.IsTrue(result.Content.NewsContentText!.Contains("正文第一段。"));
        Assert.AreEqual(1, result.Images.Count);
        Assert.AreEqual("https://img.eastmoney.com/demo1.jpg", result.Images[0].ImageResourceUrl);
        Assert.AreEqual("demo1.jpg", result.Images[0].ImageName);
        Assert.AreEqual(NewsUrl, result.Images[0].NewsUrl);
    }
}
