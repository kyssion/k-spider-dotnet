using System.Net.Sockets;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.News.Web.Eastmoney;
using KSpider.Spider.News.Flash;
using KSpider.Spider.News.Web;
using KSpider.Spider.News.Web.Cls;
using KSpider.Spider.News.Web.Sina;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     真实接口连通性测试 : 直接请求线上 URL , 验证"能调通 + 能拿到数据集 + 字段完整"。
///     与夹具解析回归分开 :
///     <list type="bullet">
///         <item>`dotnet test` 默认包含本类 , 用于人工验证线上接口是否可用</item>
///         <item>`./scripts/verify.sh` 与 CI 用 `--filter "TestCategory!=Live"` 排除 , 保持离线确定性</item>
///         <item>网络不可达 / 超时报告为跳过 ( Inconclusive ) ; 但接口能连上却返回错误、拿不到数据一律判失败</item>
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
    public async Task DfNewsLiveFetchListOriginAndParse()
    {
        var spider = new DfNewsSpider();
        var column = spider.Columns[0];

        var listPage = await FetchOrSkipAsync(() => spider.GetListPage(column, 20, null));
        Assert.IsTrue(listPage.Items.Count > 0, "东财列表接口未返回任何数据");
        AssertRealNewsRows(listPage.Items.Select(item => (item.NewsUrl, item.NewsTitle, item.NewsFrom,
            item.NewsTime, item.FromMedia, item.Category)).ToList());

        // 详情 : 真实下载原始内容 , 再解析成结构化正文
        var newsItem = listPage.Items[0];
        var origin = await FetchOrSkipAsync(() => spider.GetContentOrigin(newsItem));
        Assert.AreEqual(NewsContentOriginStatus.Success, origin.Status, $"原始内容下载失败 : {origin.Message}");

        var parseResult = spider.ParseContent(origin.NewsOriginContent, newsItem.NewsUrl ?? "");
        Assert.IsFalse(string.IsNullOrWhiteSpace(parseResult.Content.NewsTitle), "解析后标题为空");
        Assert.IsFalse(string.IsNullOrWhiteSpace(parseResult.Content.NewsContentText), "解析后正文为空");

        TestContext.WriteLine($"东财 {column.ColumnName} : {listPage.Items.Count} 条 , 样例 {newsItem.NewsTitle}");
    }

    [TestMethod]
    public async Task ClsTelegraphLiveFetchFlashPage()
    {
        await CheckFlashSourceLiveAsync(new KSpider.Spider.News.Flash.Cls.ClsNewsSpider(), "财联社电报");
    }

    [TestMethod]
    public async Task SinaLiveFetchFlashPage()
    {
        await CheckFlashSourceLiveAsync(new KSpider.Spider.News.Flash.Sina.SinaNewsSpider(), "新浪 7x24");
    }

    [TestMethod]
    public async Task WscnLiveFetchFlashPage()
    {
        await CheckFlashSourceLiveAsync(new KSpider.Spider.News.Flash.Wscn.WscnNewsSpider(), "华尔街见闻 live");
    }

    [TestMethod]
    public async Task Jin10LiveFetchFlashPage()
    {
        await CheckFlashSourceLiveAsync(new KSpider.Spider.News.Flash.Jin10.Jin10NewsSpider(), "金十快讯");
    }

    [TestMethod]
    public async Task ClsArticleLiveFetchListOriginAndParse()
    {
        var spider = new ClsArticleSpider();
        var column = spider.Columns[0];

        var listPage = await FetchOrSkipAsync(() => spider.GetListPage(column, 20, null));
        Assert.IsTrue(listPage.Items.Count > 0, "财联社文章频道列表接口未返回任何数据");
        AssertRealNewsRows(listPage.Items.Select(item => (item.NewsUrl, item.NewsTitle, item.NewsFrom,
            item.NewsTime, item.FromMedia, item.Category)).ToList());

        // 详情 : 真实下载原始内容 ( 详情页 __NEXT_DATA__ ) , 再解析成结构化正文
        var newsItem = listPage.Items[0];
        var origin = await FetchOrSkipAsync(() => spider.GetContentOrigin(newsItem));
        Assert.AreEqual(NewsContentOriginStatus.Success, origin.Status, $"原始内容下载失败 : {origin.Message}");

        var parseResult = spider.ParseContent(origin.NewsOriginContent, newsItem.NewsUrl ?? "");
        Assert.IsFalse(string.IsNullOrWhiteSpace(parseResult.Content.NewsTitle), "解析后标题为空");
        // 投教频道存在纯图片公告 ( 正文只有一个 img 段落 , 实测 id=2492095 ) : 文本与图片有其一即为有效解析
        Assert.IsTrue(!string.IsNullOrWhiteSpace(parseResult.Content.NewsContentText) || parseResult.Images.Count > 0,
            "解析后正文与图片均为空");

        // 游标续拉 : 用第一页游标能取到后续数据
        if (listPage.NextCursor != null)
        {
            var olderPage = await FetchOrSkipAsync(() => spider.GetListPage(column, 20, listPage.NextCursor));
            Assert.IsTrue(olderPage.Items.Count > 0, "用游标翻页未取到数据 ( 游标语义可能已变更 )");
        }

        TestContext.WriteLine($"财联社文章 {column.ColumnName} : {listPage.Items.Count} 条 , 样例 {newsItem.NewsTitle}");
    }

    [TestMethod]
    public async Task SinaArticleLiveFetchListOriginAndParse()
    {
        var spider = new SinaArticleSpider();

        // 体系 ① : 财经滚动接口 , 页码游标续拉
        var rollColumn = spider.Columns[0];
        var rollPage = await FetchOrSkipAsync(() => spider.GetListPage(rollColumn, 20, null));
        Assert.IsTrue(rollPage.Items.Count > 0, "新浪财经滚动接口未返回任何数据");
        AssertRealNewsRows(rollPage.Items.Select(item => (item.NewsUrl, item.NewsTitle, item.NewsFrom,
            item.NewsTime, item.FromMedia, item.Category)).ToList());
        if (rollPage.NextCursor != null)
        {
            var olderPage = await FetchOrSkipAsync(() => spider.GetListPage(rollColumn, 20, rollPage.NextCursor));
            Assert.IsTrue(olderPage.Items.Count > 0, "用页码游标翻页未取到数据 ( 游标语义可能已变更 )");
        }

        // 体系 ② : 栏目滚动页 , 整页即完整列表 ( NextCursor 恒为 null )
        var columnColumn = spider.Columns.First(column => column.ColumnId.StartsWith("page-"));
        var columnPage = await FetchOrSkipAsync(() => spider.GetListPage(columnColumn, 20, null));
        Assert.IsTrue(columnPage.Items.Count > 0, $"新浪栏目滚动页未返回任何数据 : {columnColumn.ColumnId}");
        Assert.IsNull(columnPage.NextCursor, "栏目滚动页不应有下一页游标");
        // 整页是栏目存量 ( 低频栏目最老条目可回溯数月 ) , 不能用最老条目判"陈旧" :
        // 字段完整性走共享校验 , 新鲜度只看最新一条是否在近窗口内 ( 页面仍在更新 )
        var columnRows = columnPage.Items.Select(item => (item.NewsUrl, item.NewsTitle, item.NewsFrom,
            item.NewsTime, item.FromMedia, item.Category)).ToList();
        AssertNewsFieldsComplete(columnRows);
        var columnNewest = columnRows.Max(item => item.NewsTime!.Value);
        Assert.IsTrue(columnNewest >= DateTime.Now - RecentWindow,
            $"栏目页最新一条距今超过 {RecentWindow.TotalDays} 天 , 页面可能停更 : {columnNewest}");

        // 详情 : 真实下载原始内容 ( 详情页整页 HTML ) , 再解析成结构化正文
        var newsItem = rollPage.Items[0];
        var origin = await FetchOrSkipAsync(() => spider.GetContentOrigin(newsItem));
        Assert.AreEqual(NewsContentOriginStatus.Success, origin.Status, $"原始内容下载失败 : {origin.Message}");

        var parseResult = spider.ParseContent(origin.NewsOriginContent, newsItem.NewsUrl ?? "");
        Assert.IsFalse(string.IsNullOrWhiteSpace(parseResult.Content.NewsTitle), "解析后标题为空");
        // 滚动流混有视频条目 ( 正文可能只有播放器无文本 ) : 文本与图片有其一即为有效解析
        Assert.IsTrue(!string.IsNullOrWhiteSpace(parseResult.Content.NewsContentText) || parseResult.Images.Count > 0,
            "解析后正文与图片均为空");

        TestContext.WriteLine(
            $"新浪文章 {rollColumn.ColumnName}/{columnColumn.ColumnName} : 滚动 {rollPage.Items.Count} 条 + 栏目 {columnPage.Items.Count} 条 , 样例 {newsItem.NewsTitle}");
    }

    /// <summary>
    ///     快讯源的通用连通性检查 : 拉一页完整记录 → 字段完整性 → 用游标再拉一页并确保更早
    /// </summary>
    private async Task CheckFlashSourceLiveAsync(IFlashNewsSpider spider, string sourceName)
    {
        var page = await FetchOrSkipAsync(() => spider.GetFlashPage(spider.Columns[0], 20, null));
        Assert.IsTrue(page.Items.Count > 0, $"{sourceName} 接口未返回任何数据");
        AssertRealNewsRows(page.Items.Select(item =>
            ((string?)item.NewsUrl, (string?)item.Title, (string?)"快讯", (DateTime?)item.NewsTime,
                (int?)item.FromMedia, item.Category)).ToList());
        Assert.IsTrue(page.Items.All(item => !string.IsNullOrWhiteSpace(item.Content)), $"{sourceName} 存在空正文");
        Assert.IsTrue(page.Items.All(item => !string.IsNullOrWhiteSpace(item.RawContent)),
            $"{sourceName} 存在空原始内容");

        var newest = page.Items.Max(item => item.NewsTime);
        TestContext.WriteLine($"{sourceName} : {page.Items.Count} 条 , 最新 {newest:MM-dd HH:mm:ss} , " +
                              $"样例 {page.Items[0].Title}");

        if (page.NextCursor == null) return;
        var olderPage = await FetchOrSkipAsync(() => spider.GetFlashPage(spider.Columns[0], 20, page.NextCursor));
        Assert.IsTrue(olderPage.Items.Count > 0, $"{sourceName} 用游标翻页未取到数据 ( 游标语义可能已变更 )");
        var oldestOfFirstPage = page.Items.Min(item => item.NewsTime);
        Assert.IsTrue(olderPage.Items.All(item => item.NewsTime <= oldestOfFirstPage),
            $"{sourceName} 第二页出现了比第一页更新的数据");
    }

    /// <summary>
    ///     真实返回的行必须字段完整、时间在近期
    /// </summary>
    private static void AssertRealNewsRows(
        List<(string? Url, string? Title, string? From, DateTime? Time, int? FromMedia, int Category)> rows)
    {
        AssertNewsFieldsComplete(rows);

        var newest = rows.Max(item => item.Time!.Value);
        var oldest = rows.Min(item => item.Time!.Value);
        Assert.IsTrue(newest <= DateTime.Now.AddHours(1), $"新闻时间晚于当前时间 : {newest}");
        Assert.IsTrue(oldest >= DateTime.Now - RecentWindow,
            $"新闻时间超过 {RecentWindow.TotalDays} 天 , 接口可能返回了陈旧数据 : {oldest}");
    }

    /// <summary>
    ///     字段完整性校验 ( 不含时间窗口 ) : 时间窗口对不同形态的列表语义不同 , 由调用方叠加
    /// </summary>
    private static void AssertNewsFieldsComplete(
        List<(string? Url, string? Title, string? From, DateTime? Time, int? FromMedia, int Category)> rows)
    {
        Assert.IsTrue(rows.All(item => !string.IsNullOrWhiteSpace(item.Url)), "存在空 news_url");
        // 注意不校验标题 : 实时数据存在极少数空标题条目 ( 见闻的纯图/视频条目、金十 PLUS 锁定且无 vip_title ) ,
        // 夹具回归里已按真实数据锁定过标题兜底规则 , 这里只保证结构字段完整
        Assert.IsTrue(rows.All(item => !string.IsNullOrWhiteSpace(item.From)), "存在空来源");
        Assert.IsTrue(rows.All(item => item.Time.HasValue), "存在未解析出时间的新闻");
        Assert.IsTrue(rows.All(item => item.FromMedia is > 0), "存在未设置 from_media 的新闻");
        Assert.IsTrue(rows.All(item => item.Category > 0), "存在未设置 category 的新闻");
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
