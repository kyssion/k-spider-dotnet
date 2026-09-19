using System.Text.Json.Nodes;
using KSpider.Exceptions;
using KSpider.Common.Logger;
using KSpider.Model;
using KSpider.Spider.News.Flash;
using KSpider.Spider.News.Web;
using KSpider.Spider.News.Flash.Sina.Model;
using KSpider.Tool.Http;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.News.Flash.Sina;

/// <summary>
///     新浪财经 7x24 快讯源 : 列表接口即全文 , 一次拉取直接产出完整快讯记录
/// </summary>
public class SinaNewsSpider : IFlashNewsSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<SinaNewsSpider>();

    public FromTypeOfNews FromMedia => FromTypeOfNews.SinaMedia;

    public IReadOnlyList<NewsColumn> Columns { get; } = [SinaNewsResource.LiveColumn];

    public async Task<FlashNewsPage> GetFlashPage(NewsColumn column, int pageSize, string? cursor)
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
            return ParseFlashPage(responseString, requestSize, pageNumber);
        }
        catch (Exception e)
        {
            var message =
                $"[SinaNewsSpider GetFlashPage] 拉取 7x24 快讯失败 , column : {column.ColumnId} , page : {pageNumber} , url : {url} , err : {e}";
            Log.LogError(message);
            throw new HtmlFormException(url, message, e);
        }
    }

    /// <summary>
    ///     解析列表响应 ( 独立成公开静态方法供离线测试 ) ; 单页数据即完整记录
    /// </summary>
    public static FlashNewsPage ParseFlashPage(string responseString, int requestSize, int pageNumber)
    {
        var jsonNode = JsonNode.Parse(responseString) ?? throw new HtmlFormException(SinaNewsResource.FeedUrl,
            "[SinaNewsSpider ParseFlashPage] 响应不是合法 JSON");
        if (jsonNode["result"]?["status"]?["code"]?.ToString() != "0")
            throw new HtmlFormException(SinaNewsResource.FeedUrl,
                $"[SinaNewsSpider ParseFlashPage] 接口返回错误 : {jsonNode["result"]?["status"]?["msg"]}");
        if (jsonNode["result"]?["data"]?["feed"]?["list"] is not JsonArray feedList)
            throw new HtmlFormException(SinaNewsResource.FeedUrl,
                "[SinaNewsSpider ParseFlashPage] 响应缺少 feed.list");

        var items = new List<SpiderFlashNewsModel>();
        foreach (var node in feedList)
        {
            if (node == null) continue;
            var liveItem = SinaLiveItem.FromJson(node);
            if (liveItem.Id == 0) continue;
            items.Add(liveItem.ToFlashNewsModel(node.ToJsonString()));
        }

        return new FlashNewsPage
        {
            Items = items,
            // 返回满页说明后面还有存量 , 短页即末页
            NextCursor = items.Count < requestSize ? null : (pageNumber + 1).ToString()
        };
    }
}
