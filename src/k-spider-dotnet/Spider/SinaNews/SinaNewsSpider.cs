using System.Text.Json.Nodes;
using KSpider.Exceptions;
using KSpider.Logger;
using KSpider.Model;
using KSpider.Spider.News;
using KSpider.Spider.SinaNews.Model;
using KSpider.Tool.Http;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.SinaNews;

/// <summary>
///     新浪财经 7x24 新闻源 : 直播接口已带全文 , 原始内容随列表一并落库
/// </summary>
public class SinaNewsSpider : INewsSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<SinaNewsSpider>();

    public FromTypeOfNews FromMedia => FromTypeOfNews.SinaMedia;

    public IReadOnlyList<NewsColumn> Columns { get; } = [SinaNewsResource.LiveColumn];

    public async Task<NewsListPage> GetListPage(NewsColumn column, int pageSize, string? cursor)
    {
        var requestSize = Math.Min(pageSize, SinaNewsResource.MaxPageSize);
        // 新浪按页码翻页 , 游标即页码字符串
        var pageNumber = cursor == null ? 1 : int.Parse(cursor);
        var url = $"{SinaNewsResource.FeedUrl}?page={pageNumber}&page_size={requestSize}" +
                  $"&zhibo_id={SinaNewsResource.ZhiboId}&tag_id=0&dire=f&dpc=1";
        try
        {
            var responseString = await HttpClientTools.CreateByHost(SinaNewsResource.ResourceHost)
                .GetStringAsync(url);
            return ParseListPage(responseString, requestSize, pageNumber);
        }
        catch (Exception e)
        {
            var message =
                $"[SinaNewsSpider GetListPage] 拉取 7x24 快讯失败 , column : {column.ColumnId} , page : {pageNumber} , url : {url} , err : {e}";
            Log.LogError(message);
            throw new HtmlFormException(url, message, e);
        }
    }

    /// <summary>
    ///     解析列表响应 ( 独立成公开静态方法供离线测试 ) ; 原始内容与列表项一一对应 , 由列表任务同一事务落库
    /// </summary>
    public static NewsListPage ParseListPage(string responseString, int requestSize, int pageNumber)
    {
        var jsonNode = JsonNode.Parse(responseString) ?? throw new HtmlFormException(SinaNewsResource.FeedUrl,
            "[SinaNewsSpider ParseListPage] 响应不是合法 JSON");
        if (jsonNode["result"]?["status"]?["code"]?.ToString() != "0")
            throw new HtmlFormException(SinaNewsResource.FeedUrl,
                $"[SinaNewsSpider ParseListPage] 接口返回错误 : {jsonNode["result"]?["status"]?["msg"]}");
        if (jsonNode["result"]?["data"]?["feed"]?["list"] is not JsonArray feedList)
            throw new HtmlFormException(SinaNewsResource.FeedUrl,
                "[SinaNewsSpider ParseListPage] 响应缺少 feed.list");

        var items = new List<SpiderNewsListModel>();
        var origins = new List<NewsContentOrigin>();
        foreach (var node in feedList)
        {
            if (node == null) continue;
            var liveItem = SinaLiveItem.FromJson(node);
            if (liveItem.Id == 0) continue;
            items.Add(liveItem.ToSpiderNewListModel());
            origins.Add(liveItem.ToContentOrigin(node.ToJsonString()));
        }

        return new NewsListPage
        {
            Items = items,
            InlineOrigins = origins,
            // 返回满页说明后面还有存量 , 短页即末页
            NextCursor = items.Count < requestSize ? null : (pageNumber + 1).ToString()
        };
    }

    /// <summary>
    ///     快讯原始内容随列表一并落库 , 这里没有按 id 重拉的接口 ;
    ///     仅在 origin 丢失的异常路径被调用 , 直接返回失败态 , 不假装成功
    /// </summary>
    public Task<NewsContentOrigin> GetContentOrigin(SpiderNewsListModel newsItem)
    {
        var message = $"新浪 7x24 快讯不支持按条重拉原始内容 , 原始内容随列表写入 , url : {newsItem.NewsUrl}";
        Log.LogError("[SinaNewsSpider GetContentOrigin] {Message}", message);
        return Task.FromResult(new NewsContentOrigin
        {
            NewsUrl = newsItem.NewsUrl ?? "",
            OriginType = NewsContentOriginType.Json,
            NewsOriginContent = "",
            Status = NewsContentOriginStatus.Failed,
            Message = message
        });
    }

    public NewsContentParseResult ParseContent(string originContent, string newsUrl)
    {
        var jsonNode = JsonNode.Parse(originContent) ?? throw new HtmlFormException(newsUrl,
            $"[SinaNewsSpider ParseContent] 原始内容不是合法 JSON , url : {newsUrl}");
        return SinaLiveItem.FromJson(jsonNode).ToParseResult();
    }
}
