using System.Text.Json.Nodes;
using KSpider.Exceptions;
using KSpider.Logger;
using KSpider.Model;
using KSpider.Spider.ClsNews.Model;
using KSpider.Spider.News;
using KSpider.Tool.Http;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.ClsNews;

/// <summary>
///     财联社电报新闻源 : 列表接口已带全文 , 原始内容随列表一并落库 ( 该接口没有按 id 重拉的形态 )
/// </summary>
public class ClsNewsSpider : INewsSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<ClsNewsSpider>();

    public FromTypeOfNews FromMedia => FromTypeOfNews.ClsMedia;

    public IReadOnlyList<NewsColumn> Columns { get; } = [ClsNewsResource.TelegraphColumn];

    public async Task<NewsListPage> GetListPage(NewsColumn column, int pageSize, string? cursor)
    {
        // 单页上限 50 , 超过会被静默返回空数组
        var requestSize = Math.Min(pageSize, ClsNewsResource.MaxPageSize);
        var lastTime = string.IsNullOrEmpty(cursor) ? "0" : cursor;
        var parameters = new Dictionary<string, string>
        {
            ["app"] = ClsNewsResource.App,
            ["os"] = ClsNewsResource.Os,
            ["sv"] = ClsNewsResource.Sv,
            ["refresh_type"] = "1",
            ["rn"] = requestSize.ToString(),
            ["last_time"] = lastTime
        };
        var queryString = ClsSignature.BuildQueryString(parameters);
        var url = $"{ClsNewsResource.RollListUrl}?{queryString}&sign={ClsSignature.Sign(queryString)}";
        try
        {
            var responseString = await HttpClientTools.CreateByHost(ClsNewsResource.ResourceHost).GetStringAsync(url);
            return ParseListPage(responseString, requestSize);
        }
        catch (Exception e)
        {
            var message =
                $"[ClsNewsSpider GetListPage] 拉取电报列表失败 , column : {column.ColumnId} , last_time : {lastTime} , url : {url} , err : {e}";
            Log.LogError(message);
            throw new HtmlFormException(url, message, e);
        }
    }

    /// <summary>
    ///     解析列表响应 ( 独立成公开静态方法供离线测试 ) ; 原始内容与列表项一一对应 , 由列表任务同一事务落库
    /// </summary>
    public static NewsListPage ParseListPage(string responseString, int requestSize)
    {
        var jsonNode = JsonNode.Parse(responseString) ?? throw new HtmlFormException(ClsNewsResource.RollListUrl,
            "[ClsNewsSpider ParseListPage] 响应不是合法 JSON");
        var errno = jsonNode["errno"]?.ToString();
        if (errno != "0")
            throw new HtmlFormException(ClsNewsResource.RollListUrl,
                $"[ClsNewsSpider ParseListPage] 接口返回错误 errno : {errno} , msg : {jsonNode["msg"]}");
        if (jsonNode["data"]?["roll_data"] is not JsonArray rollData)
            throw new HtmlFormException(ClsNewsResource.RollListUrl,
                "[ClsNewsSpider ParseListPage] 响应缺少 roll_data");

        var items = new List<SpiderNewsListModel>();
        var origins = new List<NewsContentOrigin>();
        var oldestCtime = long.MaxValue;
        foreach (var node in rollData)
        {
            if (node == null) continue;
            var rollItem = ClsRollItem.FromJson(node);
            if (rollItem.Id == 0) continue;
            items.Add(rollItem.ToSpiderNewListModel());
            origins.Add(rollItem.ToContentOrigin(node.ToJsonString()));
            oldestCtime = Math.Min(oldestCtime, rollItem.Ctime);
        }

        return new NewsListPage
        {
            Items = items,
            InlineOrigins = origins,
            // 游标取本页最老一条 ctime + 1 : 接口是严格小于语义 , 不加 1 会漏掉同一秒的其它条目
            // ( 边界条目会被下一页重复返回 , 由 ON CONFLICT DO NOTHING 吸收 )
            NextCursor = items.Count < requestSize ? null : (oldestCtime + 1).ToString()
        };
    }

    /// <summary>
    ///     电报原始内容随列表一并落库 , 这里没有按 id 重拉的接口 ;
    ///     仅在 origin 丢失的异常路径被调用 , 直接返回失败态 , 不假装成功
    /// </summary>
    public Task<NewsContentOrigin> GetContentOrigin(SpiderNewsListModel newsItem)
    {
        var message = $"财联社电报不支持按条重拉原始内容 , 原始内容随列表写入 , url : {newsItem.NewsUrl}";
        Log.LogError("[ClsNewsSpider GetContentOrigin] {Message}", message);
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
            $"[ClsNewsSpider ParseContent] 原始内容不是合法 JSON , url : {newsUrl}");
        return ClsRollItem.FromJson(jsonNode).ToParseResult();
    }
}
