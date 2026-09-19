using System.Text.Json.Nodes;
using KSpider.Exceptions;
using KSpider.Logger;
using KSpider.Model;
using KSpider.Spider.News;
using KSpider.Spider.WscnNews.Model;
using KSpider.Tool.Http;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.WscnNews;

/// <summary>
///     华尔街见闻 live 新闻源 : 快讯接口已带全文 , 原始内容随列表一并落库
/// </summary>
public class WscnNewsSpider : INewsSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<WscnNewsSpider>();

    public FromTypeOfNews FromMedia => FromTypeOfNews.WscnMedia;

    public IReadOnlyList<NewsColumn> Columns { get; } = [WscnNewsResource.GlobalColumn];

    public async Task<NewsListPage> GetListPage(NewsColumn column, int pageSize, string? cursor)
    {
        var requestSize = Math.Min(pageSize, WscnNewsResource.MaxPageSize);
        // 见闻用响应里给出的 next_cursor 翻页
        // 注意 : 频道值取自常量而非 column.ColumnId —— ColumnId 是 "global" , 而接口要 "global-channel"
        var cursorParam = string.IsNullOrEmpty(cursor) ? "" : $"&cursor={cursor}";
        var url = $"{WscnNewsResource.LivesUrl}?channel={WscnNewsResource.GlobalChannel}" +
                  $"&client={WscnNewsResource.Client}&limit={requestSize}{cursorParam}";
        try
        {
            var responseString = await HttpClientTools.CreateByHost(WscnNewsResource.ResourceHost)
                .GetStringAsync(url);
            return ParseListPage(responseString);
        }
        catch (Exception e)
        {
            var message =
                $"[WscnNewsSpider GetListPage] 拉取 live 快讯失败 , column : {column.ColumnId} , cursor : {cursor} , url : {url} , err : {e}";
            Log.LogError(message);
            throw new HtmlFormException(url, message, e);
        }
    }

    /// <summary>
    ///     解析列表响应 ( 独立成公开静态方法供离线测试 ) ; 原始内容与列表项一一对应 , 由列表任务同一事务落库
    /// </summary>
    public static NewsListPage ParseListPage(string responseString)
    {
        var jsonNode = JsonNode.Parse(responseString) ?? throw new HtmlFormException(WscnNewsResource.LivesUrl,
            "[WscnNewsSpider ParseListPage] 响应不是合法 JSON");
        if (jsonNode["code"]?.ToString() != "20000")
            throw new HtmlFormException(WscnNewsResource.LivesUrl,
                $"[WscnNewsSpider ParseListPage] 接口返回错误 : {jsonNode["code"]} {jsonNode["message"]}");
        if (jsonNode["data"]?["items"] is not JsonArray liveItems)
            throw new HtmlFormException(WscnNewsResource.LivesUrl,
                "[WscnNewsSpider ParseListPage] 响应缺少 data.items");

        var items = new List<SpiderNewsListModel>();
        var origins = new List<NewsContentOrigin>();
        foreach (var node in liveItems)
        {
            if (node == null) continue;
            var liveItem = WscnLiveItem.FromJson(node);
            if (liveItem.Id == 0) continue;
            items.Add(liveItem.ToSpiderNewListModel());
            origins.Add(liveItem.ToContentOrigin(node.ToJsonString()));
        }

        return new NewsListPage
        {
            Items = items,
            InlineOrigins = origins,
            // 接口自带下一页游标 , 本页没有数据时视为翻到底
            NextCursor = items.Count == 0 ? null : jsonNode["data"]?["next_cursor"]?.ToString()
        };
    }

    /// <summary>
    ///     快讯原始内容随列表一并落库 , 这里没有按 id 重拉的接口 ;
    ///     仅在 origin 丢失的异常路径被调用 , 直接返回失败态 , 不假装成功
    /// </summary>
    public Task<NewsContentOrigin> GetContentOrigin(SpiderNewsListModel newsItem)
    {
        var message = $"华尔街见闻 live 不支持按条重拉原始内容 , 原始内容随列表写入 , url : {newsItem.NewsUrl}";
        Log.LogError("[WscnNewsSpider GetContentOrigin] {Message}", message);
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
            $"[WscnNewsSpider ParseContent] 原始内容不是合法 JSON , url : {newsUrl}");
        return WscnLiveItem.FromJson(jsonNode).ToParseResult();
    }
}
