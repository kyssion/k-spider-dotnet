using System.Text.Json.Nodes;
using KSpider.Exceptions;
using KSpider.Logger;
using KSpider.Model;
using KSpider.Spider.Jin10News.Model;
using KSpider.Spider.News;
using KSpider.Tool.Http;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.Jin10News;

/// <summary>
///     金十数据快讯源 : 快讯接口已带全文 , 原始内容随列表一并落库
/// </summary>
public class Jin10NewsSpider : INewsSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<Jin10NewsSpider>();

    public FromTypeOfNews FromMedia => FromTypeOfNews.Jin10Media;

    public IReadOnlyList<NewsColumn> Columns { get; } = [Jin10NewsResource.FlashColumn];

    public async Task<NewsListPage> GetListPage(NewsColumn column, int pageSize, string? cursor)
    {
        // 接口不接受页长参数 , 固定返回约 20 条 , 这里保留参数只为与其它源签名一致
        var maxTimeParam = string.IsNullOrEmpty(cursor) ? "" : $"&max_time={Uri.EscapeDataString(cursor)}";
        var url = $"{Jin10NewsResource.FlashUrl}?channel={Jin10NewsResource.AllChannel}" +
                  $"&vip=1{maxTimeParam}";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            // 接口必须带客户端标识头 , 否则 502
            request.Headers.Add("x-app-id", Jin10NewsResource.AppId);
            request.Headers.Add("x-version", Jin10NewsResource.Version);
            using var response = await HttpClientTools.CreateByHost(Jin10NewsResource.ResourceHost)
                .SendAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new HtmlFormException(url,
                    $"[Jin10NewsSpider GetListPage] 接口返回状态码 {(int)response.StatusCode}");
            return ParseListPage(await response.Content.ReadAsStringAsync());
        }
        catch (Exception e)
        {
            var message =
                $"[Jin10NewsSpider GetListPage] 拉取快讯失败 , column : {column.ColumnId} , max_time : {cursor} , url : {url} , err : {e}";
            Log.LogError(message);
            throw new HtmlFormException(url, message, e);
        }
    }

    /// <summary>
    ///     解析列表响应 ( 独立成公开静态方法供离线测试 ) ; 原始内容与列表项一一对应 , 由列表任务同一事务落库
    /// </summary>
    public static NewsListPage ParseListPage(string responseString)
    {
        var jsonNode = JsonNode.Parse(responseString) ?? throw new HtmlFormException(Jin10NewsResource.FlashUrl,
            "[Jin10NewsSpider ParseListPage] 响应不是合法 JSON");
        if (jsonNode["status"]?.ToString() != "200")
            throw new HtmlFormException(Jin10NewsResource.FlashUrl,
                $"[Jin10NewsSpider ParseListPage] 接口返回错误 : {jsonNode["status"]} {jsonNode["message"]}");
        if (jsonNode["data"] is not JsonArray flashList)
            throw new HtmlFormException(Jin10NewsResource.FlashUrl,
                "[Jin10NewsSpider ParseListPage] 响应缺少 data");

        var items = new List<SpiderNewsListModel>();
        var origins = new List<NewsContentOrigin>();
        var oldestTime = "";
        foreach (var node in flashList)
        {
            if (node == null) continue;
            var flashItem = Jin10FlashItem.FromJson(node);
            if (flashItem.Id == "") continue;
            items.Add(flashItem.ToSpiderNewListModel());
            origins.Add(flashItem.ToContentOrigin(node.ToJsonString()));
            // 列表按时间倒序 , 循环结束时取到最老一条
            oldestTime = flashItem.Time;
        }

        return new NewsListPage
        {
            Items = items,
            InlineOrigins = origins,
            // 游标是本页最老一条的时间串 ( 接口为含边界语义 , 边界条目会被下一页重复取回 , 由入库去重吸收 )
            NextCursor = items.Count == 0 || oldestTime == "" ? null : oldestTime
        };
    }

    /// <summary>
    ///     快讯原始内容随列表一并落库 , 这里没有按 id 重拉的接口 ;
    ///     仅在 origin 丢失的异常路径被调用 , 直接返回失败态 , 不假装成功
    /// </summary>
    public Task<NewsContentOrigin> GetContentOrigin(SpiderNewsListModel newsItem)
    {
        var message = $"金十快讯不支持按条重拉原始内容 , 原始内容随列表写入 , url : {newsItem.NewsUrl}";
        Log.LogError("[Jin10NewsSpider GetContentOrigin] {Message}", message);
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
            $"[Jin10NewsSpider ParseContent] 原始内容不是合法 JSON , url : {newsUrl}");
        return Jin10FlashItem.FromJson(jsonNode).ToParseResult();
    }
}
