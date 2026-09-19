using KSpider.Spider;
using KSpider.Spider.DfNews;
using KSpider.Spider.News;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     东方财富 : 真实抓取数据的解析回归 ( 夹具为 2026-09-19 接口原样响应 , 测试离线 )
/// </summary>
[TestClass]
public class DfRealDataTest
{
    private const int ColumnNumber = 344;
    private const string ArticleUrl = "http://finance.eastmoney.com/news/1345,202609183878840472.html";

    private static string LoadFixture(string fileName)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
    }

    [TestMethod]
    public void ParseRealListResponseMapEveryItem()
    {
        var listInfos = DfListSpider.ParseListResponse(LoadFixture("df_list_344.json"), ColumnNumber);

        Assert.AreEqual(20, listInfos.Count);
        var models = listInfos.Select(item => item.ToSpiderNewListModel()).ToList();
        Assert.IsTrue(models.All(item => item.NewsUrl!.StartsWith("http")));
        Assert.IsTrue(models.All(item => !string.IsNullOrEmpty(item.NewsTitle)));
        Assert.IsTrue(models.All(item => !string.IsNullOrEmpty(item.NewsSummary)));
        Assert.IsTrue(models.All(item => !string.IsNullOrEmpty(item.NewsFrom)));
        // 列表接口时间格式 yyyy-MM-dd HH:mm:ss 必须解析成功 ( 格式不匹配会抛 FormatException )
        Assert.IsTrue(models.All(item => item.NewsTime != null));
        Assert.IsTrue(models.All(item => item.NewsTime!.Value.Year == 2026 && item.NewsTime.Value.Month == 9));
        Assert.IsTrue(models.All(item => item.FromMedia == (int)FromTypeOfNews.DfMedia));
        Assert.IsTrue(models.All(item => item.Category == ColumnNumber));
    }

    [TestMethod]
    public void ParseRealListResponseTakeCategoryFromArgument()
    {
        var listInfos = DfListSpider.ParseListResponse(LoadFixture("df_list_344.json"), 407);

        Assert.IsTrue(listInfos.All(item => item.Category == 407));
    }

    [TestMethod]
    public void ParseListResponseReturnEmptyWhenListEmpty()
    {
        // 翻页终止依赖 "空列表不抛异常"
        Assert.AreEqual(0, DfListSpider.ParseListResponse("""{"data":{"list":[]}}""", ColumnNumber).Count);
    }

    [TestMethod]
    public void ParseListResponseThrowWhenDataMissing()
    {
        Assert.ThrowsExactly<Exception>(() => DfListSpider.ParseListResponse("""{"data":null}""", ColumnNumber));
    }

    [TestMethod]
    public void ExtractArticleParamFromRealUrlForms()
    {
        // 列表接口真实返回的形态 : /news/栏目号,文章号.html
        Assert.AreEqual("202609183878840472",
            DfContentSpider.GetArticleParamFromUrl(ArticleUrl));
        // 详情页另一形态 : /a/文章号.html
        Assert.AreEqual("202609061234567",
            DfContentSpider.GetArticleParamFromUrl("https://finance.eastmoney.com/a/202609061234567.html"));
    }

    [TestMethod]
    public void RealListUrlsAreDistinctForNewsUrlDedup()
    {
        var listInfos = DfListSpider.ParseListResponse(LoadFixture("df_list_344.json"), ColumnNumber);

        var urls = listInfos.Select(item => item.NewsUrl).ToList();
        Assert.AreEqual(urls.Count, urls.Distinct().Count());
    }

    [TestMethod]
    public void ParseRealArticleJsonBuildStructuredContent()
    {
        var spider = new DfContentSpider();

        var content = spider.GetContentInfoByJson(LoadFixture("df_article_real.json"), ArticleUrl);

        Assert.IsTrue(content.NewsTitle!.Contains("美股三大指数"));
        Assert.IsTrue(content.NewsFrom!.Contains("东方财富"));
        Assert.IsFalse(string.IsNullOrEmpty(content.NewsKeyword));
        // 详情接口时间格式 yyyy/MM/dd HH:mm:ss
        Assert.IsTrue(content.NewsTime!.StartsWith("2026/09/19"));

        var model = content.ToSpiderNewsContentModel();
        Assert.AreEqual(ArticleUrl, model.NewsUrl);
        Assert.AreEqual(new DateTime(2026, 9, 19, 1, 9, 8), model.NewsTime);
        Assert.IsFalse(string.IsNullOrWhiteSpace(model.NewsContentText));
        Assert.IsTrue(model.NewsContentText!.Contains("美股三大指数"));
        Assert.IsTrue(model.NewsContentJson!.Contains(NewsContentSegment.TextType));
        Assert.IsTrue(model.NewsContentJson.Contains(NewsContentSegment.ImgType));

        Assert.IsTrue(content.NewsDataContent.Any(segment => segment.ValueType == NewsContentSegment.TextType));
        Assert.IsTrue(content.NewsDataContent.Any(segment => segment.ValueType == NewsContentSegment.ImgType));
        Assert.IsTrue(content.ImgInfos.Count > 0);
        Assert.IsTrue(content.ImgInfos.All(image => image.ResourceUrl.StartsWith("http")));
        Assert.IsTrue(content.ImgInfos.All(image => image.NewsUrl == ArticleUrl));
    }
}
