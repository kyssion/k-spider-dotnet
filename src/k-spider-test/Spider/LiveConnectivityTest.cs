using System.Net.Sockets;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.ClsNews;
using KSpider.Spider.DfNews;
using KSpider.Spider.News;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     两源真实接口连通性测试 : 直接请求线上 URL , 验证"能调通 + 能拿到数据集 + 能解析"。
///     与夹具解析回归分开 :
///     <list type="bullet">
///         <item>`dotnet test` 默认包含本类 , 用于人工验证线上接口是否可用</item>
///         <item>`./scripts/verify.sh` 与 CI 用 `--filter "TestCategory!=Live"` 排除 , 保持离线确定性</item>
///         <item>网络不可达 / 超时报告为跳过 ( Inconclusive ) ; 但接口能连上却返回错误、拿不到数据、解析失败一律判失败</item>
///     </list>
/// </summary>
[TestClass]
[TestCategory("Live")]
public class LiveConnectivityTest
{
    /// <summary>
    ///     新闻时间允许的回溯窗口 , 超过说明接口可能返回了陈旧数据
    /// </summary>
    private static readonly TimeSpan RecentWindow = TimeSpan.FromDays(7);

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task ClsTelegraphLiveFetchListCursorAndParse()
    {
        var spider = new ClsNewsSpider();
        var column = spider.Columns[0];

        var listPage = await FetchOrSkipAsync(() => spider.GetListPage(column, 20, null));
        Assert.IsTrue(listPage.Items.Count > 0, "财联社电报接口未返回任何数据");
        Assert.AreEqual(listPage.Items.Count, listPage.InlineOrigins.Count, "电报列表项与原始内容必须一一对应");
        Assert.IsNotNull(listPage.NextCursor, "返回满页时应给出下一页游标");
        AssertRealNewsItems(listPage.Items);

        // 游标翻页 : 第二页必须能取到更早的数据
        var olderPage = await FetchOrSkipAsync(() => spider.GetListPage(column, 20, listPage.NextCursor));
        Assert.IsTrue(olderPage.Items.Count > 0, "用游标翻页未取到数据 ( 游标语义可能已变更 )");
        var oldestOfFirstPage = listPage.Items.Min(item => item.NewsTime);
        Assert.IsTrue(olderPage.Items.All(item => item.NewsTime <= oldestOfFirstPage), "第二页出现了比第一页更新的数据");

        // 解析 : 用真实原始内容走一遍解析链路
        var parseResult = spider.ParseContent(listPage.InlineOrigins[0].NewsOriginContent,
            listPage.InlineOrigins[0].NewsUrl);
        Assert.IsFalse(string.IsNullOrWhiteSpace(parseResult.Content.NewsContentText), "电报解析后正文为空");
        Assert.AreEqual(listPage.InlineOrigins[0].NewsUrl, parseResult.Content.NewsUrl);

        TestContext.WriteLine(
            $"财联社电报 : 首页 {listPage.Items.Count} 条 ( {listPage.Items.Min(item => item.NewsTime):HH:mm} ~ {listPage.Items.Max(item => item.NewsTime):HH:mm} ) , 次页 {olderPage.Items.Count} 条");
        TestContext.WriteLine($"  样例 : {listPage.Items[0].NewsUrl} | {listPage.Items[0].NewsTitle}");
        TestContext.WriteLine($"  解析正文 {parseResult.Content.NewsContentText!.Length} 字 , 标签 {parseResult.Content.NewsKeyword}");
    }

    [TestMethod]
    public async Task DfNewsLiveFetchListOriginAndParse()
    {
        var spider = new DfNewsSpider();
        var column = spider.Columns[0];

        var listPage = await FetchOrSkipAsync(() => spider.GetListPage(column, 20, null));
        Assert.IsTrue(listPage.Items.Count > 0, "东财列表接口未返回任何数据");
        Assert.AreEqual(0, listPage.InlineOrigins.Count, "东财是详情页型源 , 不应带内联原始内容");
        AssertRealNewsItems(listPage.Items);

        // 详情 : 真实下载原始内容 , 再解析成结构化正文
        var newsItem = listPage.Items[0];
        var origin = await FetchOrSkipAsync(() => spider.GetContentOrigin(newsItem));
        Assert.AreEqual(NewsContentOriginStatus.Success, origin.Status, $"原始内容下载失败 : {origin.Message}");
        Assert.IsFalse(string.IsNullOrEmpty(origin.NewsOriginContent), "原始内容为空");

        var parseResult = spider.ParseContent(origin.NewsOriginContent, newsItem.NewsUrl ?? "");
        Assert.IsFalse(string.IsNullOrWhiteSpace(parseResult.Content.NewsTitle), "解析后标题为空");
        Assert.IsFalse(string.IsNullOrWhiteSpace(parseResult.Content.NewsContentText), "解析后正文为空");
        Assert.AreEqual(newsItem.NewsUrl, parseResult.Content.NewsUrl);

        TestContext.WriteLine(
            $"东财 {column.ColumnName} ( column={column.ColumnId} ) : {listPage.Items.Count} 条 , 时间 {listPage.Items.Min(item => item.NewsTime):MM-dd HH:mm} ~ {listPage.Items.Max(item => item.NewsTime):MM-dd HH:mm}");
        TestContext.WriteLine($"  样例 : {newsItem.NewsUrl} | {newsItem.NewsTitle}");
        TestContext.WriteLine(
            $"  解析正文 {parseResult.Content.NewsContentText!.Length} 字 , 图片 {parseResult.Images.Count} 张 , 关键字 {parseResult.Content.NewsKeyword}");
    }

    /// <summary>
    ///     真实返回的列表行必须字段完整、时间在近期
    /// </summary>
    private static void AssertRealNewsItems(List<SpiderNewsListModel> items)
    {
        Assert.IsTrue(items.All(item => !string.IsNullOrWhiteSpace(item.NewsUrl)), "存在空 news_url");
        Assert.IsTrue(items.All(item => !string.IsNullOrWhiteSpace(item.NewsTitle)), "存在空标题");
        Assert.IsTrue(items.All(item => !string.IsNullOrWhiteSpace(item.NewsFrom)), "存在空来源");
        Assert.IsTrue(items.All(item => item.NewsTime.HasValue), "存在未解析出时间的新闻");
        Assert.IsTrue(items.All(item => item.FromMedia is > 0), "存在未设置 from_media 的新闻");
        Assert.IsTrue(items.All(item => item.Category > 0), "存在未设置 category 的新闻");

        var newest = items.Max(item => item.NewsTime!.Value);
        var oldest = items.Min(item => item.NewsTime!.Value);
        Assert.IsTrue(newest <= DateTime.Now.AddHours(1), $"新闻时间晚于当前时间 : {newest}");
        Assert.IsTrue(oldest >= DateTime.Now - RecentWindow,
            $"新闻时间超过 {RecentWindow.TotalDays} 天 , 接口可能返回了陈旧数据 : {oldest}");
    }

    /// <summary>
    ///     网络不可达 / 超时 -> 跳过 ; 其余异常 ( 接口报错、解析失败 ) 交给用例判失败
    /// </summary>
    private static async Task<T> FetchOrSkipAsync<T>(Func<Task<T>> fetch)
    {
        try
        {
            return await fetch();
        }
        catch (Exception e) when (IsNetworkFailure(e))
        {
            Assert.Inconclusive($"网络不可达或超时 , 跳过真实接口验证 : {e.Message}");
            throw;
        }
    }

    private static bool IsNetworkFailure(Exception? exception)
    {
        while (exception != null)
        {
            if (exception is HttpRequestException or TaskCanceledException or SocketException) return true;
            exception = exception.InnerException;
        }

        return false;
    }
}
