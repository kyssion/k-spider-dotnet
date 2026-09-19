using System.Text.Json.Nodes;
using KSpider.Exceptions;
using KSpider.Logger;
using KSpider.Model;
using KSpider.Spider.ClsNews.Model;
using KSpider.Spider.FlashNews;
using KSpider.Spider.News;
using KSpider.Tool.Http;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.ClsNews;

/// <summary>
///     财联社电报快讯源 : 列表接口即全文 , 一次拉取直接产出完整快讯记录
/// </summary>
public class ClsNewsSpider : IFlashNewsSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<ClsNewsSpider>();

    public FromTypeOfNews FromMedia => FromTypeOfNews.ClsMedia;

    public IReadOnlyList<NewsColumn> Columns { get; } = [ClsNewsResource.TelegraphColumn];

    public async Task<FlashNewsPage> GetFlashPage(NewsColumn column, int pageSize, string? cursor)
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
            return ParseFlashPage(responseString, requestSize);
        }
        catch (Exception e)
        {
            var message =
                $"[ClsNewsSpider GetFlashPage] 拉取电报快讯失败 , column : {column.ColumnId} , last_time : {lastTime} , url : {url} , err : {e}";
            Log.LogError(message);
            throw new HtmlFormException(url, message, e);
        }
    }

    /// <summary>
    ///     解析列表响应 ( 独立成公开静态方法供离线测试 ) ; 单页数据即完整记录
    /// </summary>
    public static FlashNewsPage ParseFlashPage(string responseString, int requestSize)
    {
        var jsonNode = JsonNode.Parse(responseString) ?? throw new HtmlFormException(ClsNewsResource.RollListUrl,
            "[ClsNewsSpider ParseFlashPage] 响应不是合法 JSON");
        var errno = jsonNode["errno"]?.ToString();
        if (errno != "0")
            throw new HtmlFormException(ClsNewsResource.RollListUrl,
                $"[ClsNewsSpider ParseFlashPage] 接口返回错误 errno : {errno} , msg : {jsonNode["msg"]}");
        if (jsonNode["data"]?["roll_data"] is not JsonArray rollData)
            throw new HtmlFormException(ClsNewsResource.RollListUrl,
                "[ClsNewsSpider ParseFlashPage] 响应缺少 roll_data");

        var items = new List<SpiderFlashNewsModel>();
        var oldestCtime = long.MaxValue;
        foreach (var node in rollData)
        {
            if (node == null) continue;
            var rollItem = ClsRollItem.FromJson(node);
            if (rollItem.Id == 0) continue;
            items.Add(rollItem.ToFlashNewsModel(node.ToJsonString()));
            oldestCtime = Math.Min(oldestCtime, rollItem.Ctime);
        }

        return new FlashNewsPage
        {
            Items = items,
            // 游标取本页最老一条 ctime + 1 : 接口是严格小于语义 , 不加 1 会漏掉同一秒的其它条目
            // ( 边界条目会被下一页重复取回 , 由 ON CONFLICT 的 DO UPDATE 吸收 )
            NextCursor = items.Count < requestSize ? null : (oldestCtime + 1).ToString()
        };
    }
}
