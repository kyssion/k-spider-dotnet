using System.Text.Json.Nodes;
using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider.News.Web;
using KSpider.Spider.Verify;
using KSpider.Common;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.News.Flash.Gelonghui;

/// <summary>
///     格隆汇 live 快讯源 : 列表接口即全文 , 一次拉取直接产出完整快讯记录。
///     v4 接口固定返回 15 条 , liveId 游标翻页 ( 上一页最老 id , 页间零重叠 ) ;
///     timestamp 毫秒参数必带 ( 防服务端缓存恒返回同一批 ) ; 停止由任务层"当前页已全部存在即停"判定。
/// </summary>
public class GelonghuiNewsSpider : IFlashNewsSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<GelonghuiNewsSpider>();

    public FromTypeOfNews FromMedia => FromTypeOfNews.GelonghuiMedia;

    public IReadOnlyList<NewsColumn> Columns { get; } =
        [new NewsColumn("all", "live全量流")];

    public async Task<FlashNewsPage> GetFlashPage(NewsColumn column, int pageSize, string? cursor)
    {
        // liveId 游标 : 首页传空 ; timestamp 毫秒防缓存
        var liveId = cursor ?? "";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var url = string.Format(GelonghuiNewsResource.LiveUrlTemplate, liveId, timestamp);
        try
        {
            var responseString = await VerifiedHttp.GetStringAsync(GelonghuiNewsResource.ResourceHost, url);
            return ParseFlashPage(responseString);
        }
        catch (Exception e)
        {
            var message =
                $"[GelonghuiNewsSpider GetFlashPage] 拉取格隆汇 live 失败 , liveId : {liveId} , url : {url} , err : {e}";
            Log.LogError(message);
            throw new HtmlFormException(url, message, e);
        }
    }

    /// <summary>
    ///     解析列表响应 ( 独立成公开静态方法供离线测试 ) ; 固定 15 条满页 , 给下一页 liveId 游标 ,
    ///     短页即末页 ( 实测翻到旧页会不足 15 条 )
    /// </summary>
    public static FlashNewsPage ParseFlashPage(string responseString)
    {
        var jsonNode = JsonNode.Parse(responseString) ?? throw new HtmlFormException(GelonghuiNewsResource.ResourceHost,
            "[GelonghuiNewsSpider ParseFlashPage] 响应不是合法 JSON");
        if (jsonNode["statusCode"]?.ToString() != "200")
            throw new HtmlFormException(GelonghuiNewsResource.ResourceHost,
                $"[GelonghuiNewsSpider ParseFlashPage] 接口返回错误 statusCode : {jsonNode["statusCode"]} , message : {jsonNode["message"]}");
        if (jsonNode["result"] is not JsonArray liveItems)
            throw new HtmlFormException(GelonghuiNewsResource.ResourceHost,
                "[GelonghuiNewsSpider ParseFlashPage] 响应缺少 result 数组");

        var items = new List<SpiderFlashNewsModel>();
        var oldestId = long.MaxValue;
        foreach (var node in liveItems)
        {
            if (node == null) continue;
            var liveItem = GelonghuiLiveItem.FromJson(node);
            // id / route / content 缺失视为坏数据跳过 ( 无正文或无去重键不可入库 )
            if (liveItem.Id == 0 || string.IsNullOrEmpty(liveItem.NewsUrl) || string.IsNullOrEmpty(liveItem.Content))
                continue;
            items.Add(liveItem.ToFlashNewsModel(node.ToJsonString()));
            oldestId = Math.Min(oldestId, liveItem.Id);
        }

        return new FlashNewsPage
        {
            Items = items,
            NextCursor = items.Count < GelonghuiNewsResource.FixedPageSize || oldestId == long.MaxValue
                ? null
                : oldestId.ToString()
        };
    }
}
