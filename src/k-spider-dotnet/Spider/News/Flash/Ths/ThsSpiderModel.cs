using System.Text.Json.Nodes;
using KSpider.Model;
using KSpider.Spider.News.Web;
using KSpider.Common;

namespace KSpider.Spider.News.Flash.Ths;

/// <summary>
///     同花顺 7x24 快讯的单条数据 ( /tapp/news/push/stock 的 list 项 ) :
///     与电报/金十同款约定 , FromJson 一次性显式赋值 , ToFlashNewsModel 只做 1:1 映射。
///     digest 即完整全文 ( 与 short 恒等 , 实测无截断 ) , source 恒为空回退平台名 ,
///     import 两档 ( "3" 红标重要 / "0" 普通 ) 映射快讯 level 2/1。
/// </summary>
public class ThsFlashItem
{
    private static readonly TimeSpan ChinaOffset = TimeSpan.FromHours(8);

    private const int BriefTitleMaxLength = 60;

    /// <summary>seq 即站内内容 id ( 详情页地址 c{seq}.shtml 与之一致 )</summary>
    public long Seq { get; set; }

    public string Title { get; set; } = "";

    /// <summary>全文 ( digest 与 short 恒等 )</summary>
    public string Content { get; set; } = "";

    /// <summary>接口给的详情页地址 ( 含日期路径 , 实测 200 可达 ) , 作为 news_url 稳定键</summary>
    public string NewsUrl { get; set; } = "";

    public long Ctime { get; set; }

    public DateTime NewsTime => DateTimeOffset.FromUnixTimeSeconds(Ctime).ToOffset(ChinaOffset).DateTime;

    /// <summary>标签 ( 逗号分隔 , 如 "异动,A股" )</summary>
    public string Keyword { get; set; } = "";

    /// <summary>封面图 ( 低频 , 实测约 4% 条目带图 )</summary>
    public string PicUrl { get; set; } = "";

    /// <summary>关联标的原始数组 ( 提取为统一形态落 stock_list )</summary>
    public JsonArray? Stocks { get; set; }

    public short Level { get; set; }

    public static ThsFlashItem FromJson(JsonNode node)
    {
        var seq = long.TryParse(node["seq"]?.ToString(), out var seqValue) ? seqValue : 0;
        var title = node["title"]?.ToString() ?? "";
        var content = node["digest"]?.ToString() ?? "";
        return new ThsFlashItem
        {
            Seq = seq,
            Title = string.IsNullOrEmpty(title) ? Truncate(content, BriefTitleMaxLength) : title,
            Content = content,
            NewsUrl = node["url"]?.ToString() ?? "",
            Ctime = long.TryParse(node["ctime"]?.ToString(), out var ctime) ? ctime : 0,
            Keyword = node["tag"]?.ToString() ?? "",
            PicUrl = node["picUrl"]?.ToString() ?? "",
            Stocks = node["stock"] as JsonArray,
            // import 两档 : "3" 红标重要 / 其余普通 ( 实测与 color=2 恒对应 )
            Level = node["import"]?.ToString() == "3" ? (short)2 : (short)1
        };
    }

    public SpiderFlashNewsModel ToFlashNewsModel(string itemJson)
    {
        var imageUrls = string.IsNullOrEmpty(PicUrl) ? [] : new List<string> { PicUrl };
        return new SpiderFlashNewsModel
        {
            FromMedia = (int)FromTypeOfNews.ThsMedia,
            Category = ThsNewsResource.FlashCategoryNumber,
            NewsUrl = NewsUrl,
            NewsTime = NewsTime,
            Title = Title,
            Content = Content,
            Keyword = string.IsNullOrEmpty(Keyword) ? null : Keyword,
            Level = Level,
            StockList = ReadStockListJson(),
            ImageUrls = imageUrls.Count > 0 ? JsonTools.GetJson(imageUrls) : null,
            RawContent = itemJson
        };
    }

    /// <summary>
    ///     关联标的提取为统一形态 [{"stock_id":"002129","name":"TCL中环"}] ( 与财联社同款 ) ; 无标的返回 null
    /// </summary>
    private string? ReadStockListJson()
    {
        if (Stocks == null || Stocks.Count == 0) return null;
        var list = new List<JsonObject>();
        foreach (var item in Stocks)
        {
            if (item == null) continue;
            var code = item["stockCode"]?.ToString() ?? "";
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
