using System.Text.Json.Nodes;
using KSpider.Model;
using KSpider.Spider.News.Web;
using KSpider.Common;

namespace KSpider.Spider.News.Flash.Gelonghui;

/// <summary>
///     格隆汇 live 的单条快讯 ( /api/live-channels/all/lives/v4 的 result 项 ) :
///     与电报/同花顺同款约定 , FromJson 一次性显式赋值 , ToFlashNewsModel 只做 1:1 映射。
///     title 部分条目为空 ( 用正文截断兜底 ) ; content 全文自带"格隆汇x月x日｜"前缀保留原文 ;
///     level 0/1 两档映射快讯 level 1/2 ; route 自带规范详情页地址作去重键。
/// </summary>
public class GelonghuiLiveItem
{
    private static readonly TimeSpan ChinaOffset = TimeSpan.FromHours(8);

    private const int BriefTitleMaxLength = 60;

    public long Id { get; set; }

    public string Title { get; set; } = "";

    /// <summary>全文 ( 通常带"格隆汇x月x日｜"前缀 , 保留原文 )</summary>
    public string Content { get; set; } = "";

    public string NewsUrl { get; set; } = "";

    public long CreateTimestamp { get; set; }

    public DateTime NewsTime => DateTimeOffset.FromUnixTimeSeconds(CreateTimestamp).ToOffset(ChinaOffset).DateTime;

    public List<string> Pictures { get; set; } = [];

    /// <summary>关联标的原始数组 ( 提取为统一形态落 stock_list )</summary>
    public JsonArray? RelatedStocks { get; set; }

    public short Level { get; set; }

    public static GelonghuiLiveItem FromJson(JsonNode node)
    {
        var id = long.TryParse(node["id"]?.ToString(), out var idValue) ? idValue : 0;
        var title = node["title"]?.ToString() ?? "";
        var content = node["content"]?.ToString() ?? "";
        var pictures = new List<string>();
        if (node["pictures"] is JsonArray pictureArray)
            foreach (var picture in pictureArray)
            {
                // pictures 元素是字符串 URL 或含 url 键的对象 , 两种形态都兼容
                var url = picture is JsonObject
                    ? picture["url"]?.ToString()
                    : picture?.ToString();
                if (!string.IsNullOrEmpty(url)) pictures.Add(url);
            }

        return new GelonghuiLiveItem
        {
            Id = id,
            Title = string.IsNullOrEmpty(title) ? Truncate(content, BriefTitleMaxLength) : title,
            Content = content,
            NewsUrl = node["route"]?.ToString() ?? "",
            CreateTimestamp = long.TryParse(node["createTimestamp"]?.ToString(), out var ts) ? ts : 0,
            Pictures = pictures,
            RelatedStocks = node["relatedStocks"] as JsonArray,
            // level 两档 : 1 = 站内红标重要
            Level = node["level"]?.ToString() == "1" ? (short)2 : (short)1
        };
    }

    public SpiderFlashNewsModel ToFlashNewsModel(string itemJson)
    {
        return new SpiderFlashNewsModel
        {
            FromMedia = (int)FromTypeOfNews.GelonghuiMedia,
            Category = GelonghuiNewsResource.FlashCategoryNumber,
            NewsUrl = NewsUrl,
            NewsTime = NewsTime,
            Title = Title,
            Content = Content,
            Keyword = null,
            Level = Level,
            StockList = ReadStockListJson(),
            ImageUrls = Pictures.Count > 0 ? JsonTools.GetJson(Pictures) : null,
            RawContent = itemJson
        };
    }

    /// <summary>
    ///     关联标的提取为统一形态 [{"stock_id":"01797","name":"东方甄选"}] ( 与财联社同款 ) ; 无标的返回 null
    /// </summary>
    private string? ReadStockListJson()
    {
        if (RelatedStocks == null || RelatedStocks.Count == 0) return null;
        var list = new List<JsonObject>();
        foreach (var item in RelatedStocks)
        {
            if (item == null) continue;
            var code = item["code"]?.ToString() ?? "";
            if (code == "") continue;
            list.Add(new JsonObject { ["stock_id"] = code, ["name"] = item["name"]?.ToString() ?? "" });
        }

        return list.Count > 0 ? JsonTools.GetJson(list) : null;
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
