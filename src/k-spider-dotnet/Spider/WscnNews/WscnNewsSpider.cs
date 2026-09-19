using System.Text.Json.Nodes;
using KSpider.Exceptions;
using KSpider.Logger;
using KSpider.Model;
using KSpider.Spider.FlashNews;
using KSpider.Spider.News;
using KSpider.Spider.WscnNews.Model;
using KSpider.Tool.Http;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.WscnNews;

/// <summary>
///     华尔街见闻 live 快讯源 : 列表接口即全文 , 一次拉取直接产出完整快讯记录
/// </summary>
public class WscnNewsSpider : IFlashNewsSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<WscnNewsSpider>();

    public FromTypeOfNews FromMedia => FromTypeOfNews.WscnMedia;

    public IReadOnlyList<NewsColumn> Columns { get; } = [WscnNewsResource.GlobalColumn];

    public async Task<FlashNewsPage> GetFlashPage(NewsColumn column, int pageSize, string? cursor)
    {
        var requestSize = Math.Min(pageSize, WscnNewsResource.MaxPageSize);
        // 见闻用响应里给出的 next_cursor 翻页
        var cursorParam = string.IsNullOrEmpty(cursor) ? "" : $"&cursor={cursor}";
        var url = $"{WscnNewsResource.LivesUrl}?channel={WscnNewsResource.GlobalChannel}" +
                  $"&client={WscnNewsResource.Client}&limit={requestSize}{cursorParam}";
        try
        {
            var responseString = await HttpClientTools.CreateByHost(WscnNewsResource.ResourceHost)
                .GetStringAsync(url);
            return ParseFlashPage(responseString);
        }
        catch (Exception e)
        {
            var message =
                $"[WscnNewsSpider GetFlashPage] 拉取 live 快讯失败 , column : {column.ColumnId} , cursor : {cursor} , url : {url} , err : {e}";
            Log.LogError(message);
            throw new HtmlFormException(url, message, e);
        }
    }

    /// <summary>
    ///     解析列表响应 ( 独立成公开静态方法供离线测试 ) ; 单页数据即完整记录
    /// </summary>
    public static FlashNewsPage ParseFlashPage(string responseString)
    {
        var jsonNode = JsonNode.Parse(responseString) ?? throw new HtmlFormException(WscnNewsResource.LivesUrl,
            "[WscnNewsSpider ParseFlashPage] 响应不是合法 JSON");
        if (jsonNode["code"]?.ToString() != "20000")
            throw new HtmlFormException(WscnNewsResource.LivesUrl,
                $"[WscnNewsSpider ParseFlashPage] 接口返回错误 : {jsonNode["code"]} {jsonNode["message"]}");
        if (jsonNode["data"]?["items"] is not JsonArray liveItems)
            throw new HtmlFormException(WscnNewsResource.LivesUrl,
                "[WscnNewsSpider ParseFlashPage] 响应缺少 data.items");

        var items = new List<SpiderFlashNewsModel>();
        foreach (var node in liveItems)
        {
            if (node == null) continue;
            var liveItem = WscnLiveItem.FromJson(node);
            if (liveItem.Id == 0) continue;
            items.Add(liveItem.ToFlashNewsModel(node.ToJsonString()));
        }

        return new FlashNewsPage
        {
            Items = items,
            // 接口自带下一页游标 , 本页没有数据时视为翻到底
            NextCursor = items.Count == 0 ? null : jsonNode["data"]?["next_cursor"]?.ToString()
        };
    }
}
