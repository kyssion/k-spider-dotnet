using System.Text.Json.Nodes;
using KSpider.Exceptions;
using KSpider.Common.Logger;
using KSpider.Model;
using KSpider.Spider.News.Flash;
using KSpider.Spider.News.Web;
using KSpider.Spider.News.Flash.Jin10.Model;
using KSpider.Tool.Http;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.News.Flash.Jin10;

/// <summary>
///     金十数据快讯源 : 列表接口即全文 , 一次拉取直接产出完整快讯记录
/// </summary>
public class Jin10NewsSpider : IFlashNewsSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<Jin10NewsSpider>();

    public FromTypeOfNews FromMedia => FromTypeOfNews.Jin10Media;

    public IReadOnlyList<NewsColumn> Columns { get; } = [Jin10NewsResource.FlashColumn];

    public async Task<FlashNewsPage> GetFlashPage(NewsColumn column, int pageSize, string? cursor)
    {
        // 接口不接受页长参数 , 固定返回约 20 条 , pageSize 仅作翻到末页的判断基准
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
                    $"[Jin10NewsSpider GetFlashPage] 接口返回状态码 {(int)response.StatusCode}");
            return ParseFlashPage(await response.Content.ReadAsStringAsync(), pageSize);
        }
        catch (Exception e)
        {
            var message =
                $"[Jin10NewsSpider GetFlashPage] 拉取快讯失败 , column : {column.ColumnId} , max_time : {cursor} , url : {url} , err : {e}";
            Log.LogError(message);
            throw new HtmlFormException(url, message, e);
        }
    }

    /// <summary>
    ///     解析列表响应 ( 独立成公开静态方法供离线测试 ) ; 单页数据即完整记录
    /// </summary>
    public static FlashNewsPage ParseFlashPage(string responseString, int requestSize)
    {
        var jsonNode = JsonNode.Parse(responseString) ?? throw new HtmlFormException(Jin10NewsResource.FlashUrl,
            "[Jin10NewsSpider ParseFlashPage] 响应不是合法 JSON");
        if (jsonNode["status"]?.ToString() != "200")
            throw new HtmlFormException(Jin10NewsResource.FlashUrl,
                $"[Jin10NewsSpider ParseFlashPage] 接口返回错误 : {jsonNode["status"]} {jsonNode["message"]}");
        if (jsonNode["data"] is not JsonArray flashList)
            throw new HtmlFormException(Jin10NewsResource.FlashUrl,
                "[Jin10NewsSpider ParseFlashPage] 响应缺少 data");

        var items = new List<SpiderFlashNewsModel>();
        var oldestTime = "";
        foreach (var node in flashList)
        {
            if (node == null) continue;
            var flashItem = Jin10FlashItem.FromJson(node);
            if (flashItem.Id == "") continue;
            // PLUS 专享条目可能连 vip_title 都是空的 ( lock=true 且无任何公开信息 , 实测约 3/21 ) , 跳过不入库
            if (flashItem.HasNoPublicContent) continue;
            items.Add(flashItem.ToFlashNewsModel(node.ToJsonString()));
            // 列表按时间倒序 , 循环结束时取到最老一条
            oldestTime = flashItem.Time;
        }

        return new FlashNewsPage
        {
            Items = items,
            // 游标是本页最老一条的时间串 ( 接口为含边界语义 , 边界条目会被下一页重复取回 , 由 DO UPDATE 吸收 )
            NextCursor = items.Count == 0 || oldestTime == "" ? null : oldestTime
        };
    }
}
