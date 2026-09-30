using System.Text.Json.Nodes;
using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider.News.Web;
using KSpider.Spider.Verify;
using KSpider.Common;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.News.Flash.Ths;

/// <summary>
///     同花顺 7x24 快讯源 : 列表接口即全文 , 一次拉取直接产出完整快讯记录。
///     页码翻页 ( cursor 即下一页页码 ) , 停止由任务层"当前页已全部存在即停"判定 ,
///     页码只在满页时推进 ; tag 参数留作栏目扩展 ( 当前只接全量流 )。
/// </summary>
public class ThsNewsSpider : IFlashNewsSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<ThsNewsSpider>();

    public FromTypeOfNews FromMedia => FromTypeOfNews.ThsMedia;

    public IReadOnlyList<NewsColumn> Columns { get; } =
        [new NewsColumn("all", "7x24全量流")];

    public async Task<FlashNewsPage> GetFlashPage(NewsColumn column, int pageSize, string? cursor)
    {
        var requestSize = Math.Min(pageSize, ThsNewsResource.MaxPageSize);
        // 页码游标 : 首页从 1 起
        var page = int.TryParse(cursor, out var parsed) && parsed > 1 ? parsed : 1;
        var url = $"{ThsNewsResource.FlashListUrl}?page={page}&pagesize={requestSize}&track=website&tag=";
        try
        {
            var responseString = await VerifiedHttp.GetStringAsync(ThsNewsResource.ResourceHost, url);
            return ParseFlashPage(responseString, requestSize, page);
        }
        catch (Exception e)
        {
            var message =
                $"[ThsNewsSpider GetFlashPage] 拉取同花顺快讯失败 , page : {page} , url : {url} , err : {e}";
            Log.LogError(message);
            throw new HtmlFormException(url, message, e);
        }
    }

    /// <summary>
    ///     解析列表响应 ( 独立成公开静态方法供离线测试 ) ;
    ///     满页才给下一页游标 , 短页即末页 ( 接口按 pagesize 裁页 , 与财联社"不裁页"不同 )
    /// </summary>
    public static FlashNewsPage ParseFlashPage(string responseString, int requestSize, int page)
    {
        var jsonNode = JsonNode.Parse(responseString) ?? throw new HtmlFormException(ThsNewsResource.FlashListUrl,
            "[ThsNewsSpider ParseFlashPage] 响应不是合法 JSON");
        if (jsonNode["code"]?.ToString() != "200")
            throw new HtmlFormException(ThsNewsResource.FlashListUrl,
                $"[ThsNewsSpider ParseFlashPage] 接口返回错误 code : {jsonNode["code"]} , msg : {jsonNode["msg"]}");
        if (jsonNode["data"]?["list"] is not JsonArray listData)
            throw new HtmlFormException(ThsNewsResource.FlashListUrl,
                "[ThsNewsSpider ParseFlashPage] 响应缺少 data.list");

        var items = new List<SpiderFlashNewsModel>();
        foreach (var node in listData)
        {
            if (node == null) continue;
            var item = ThsFlashItem.FromJson(node);
            // seq ( 内容 id ) 或详情地址缺失视为坏数据跳过 ; digest 为空同样跳过 ( 无正文不可入库 )
            if (item.Seq == 0 || string.IsNullOrEmpty(item.NewsUrl) || string.IsNullOrEmpty(item.Content)) continue;
            items.Add(item.ToFlashNewsModel(node.ToJsonString()));
        }

        return new FlashNewsPage
        {
            Items = items,
            NextCursor = items.Count < requestSize ? null : (page + 1).ToString()
        };
    }
}
