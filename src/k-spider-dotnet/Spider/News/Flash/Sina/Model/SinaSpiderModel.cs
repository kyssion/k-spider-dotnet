using System.Text.Json.Nodes;
using KSpider.Common.Json;
using KSpider.Model;
using KSpider.Spider.News.Flash;
using KSpider.Common.Time;

namespace KSpider.Spider.News.Flash.Sina.Model;

/// <summary>
///     新浪 7x24 直播的单条快讯 ( 列表即全文 ) , 直接映射为可入库的快讯记录
/// </summary>
public class SinaLiveItem
{
    /// <summary>
    ///     无标题快讯用正文兜底的截断长度
    /// </summary>
    private const int TitleMaxLength = 60;

    public long Id { get; set; }

    /// <summary>
    ///     正文 , 以【标题】开头时前半段即标题 ( 实测 100 条里 86 条带【】)
    /// </summary>
    public string RichText { get; set; } = "";

    /// <summary>
    ///     发布时间 ( 北京时间字符串 , 无时区标记 )
    /// </summary>
    public string CreateTime { get; set; } = "";

    public string DocUrl { get; set; } = "";

    public List<string> Tags { get; set; } = [];

    /// <summary>
    ///     关联标的 ( ext 字段是 JSON 字符串 , 内含 stocks 数组 : symbol 代码 + key 名称 )
    /// </summary>
    public JsonArray? Stocks { get; set; }

    public string NewsUrl => string.IsNullOrEmpty(DocUrl)
        ? string.Format(SinaNewsResource.FallbackNewsUrlTemplate, Id)
        : DocUrl;

    public DateTime NewsTime => TimeTools.GetDateByTimeStrForFormat(CreateTime, TimeTools.TimeFormatForStrikethrough);

    private string DisplayTitle => ExtractTitle(RichText);

    public static SinaLiveItem FromJson(JsonNode node)
    {
        return new SinaLiveItem
        {
            Id = long.TryParse(node["id"]?.ToString(), out var id) ? id : 0,
            RichText = node["rich_text"]?.ToString() ?? "",
            CreateTime = node["create_time"]?.ToString() ?? "",
            DocUrl = node["docurl"]?.ToString() ?? "",
            Tags = ReadTagNames(node["tag"]),
            Stocks = ReadStocks(node["ext"])
        };
    }

    public SpiderFlashNewsModel ToFlashNewsModel(string itemJson)
    {
        return new SpiderFlashNewsModel
        {
            FromMedia = (int)FromTypeOfNews.SinaMedia,
            Category = SinaNewsResource.LiveCategoryNumber,
            NewsUrl = NewsUrl,
            NewsTime = NewsTime,
            Title = DisplayTitle,
            Content = RichText,
            Keyword = string.Join(",", Tags),
            Level = 1,
            StockList = ReadStockListJson(),
            // 图片在 multimedia 字段 ( 实测 100 条仅 1 条非空 ) , v1 不解析
            ImageUrls = null,
            RawContent = itemJson
        };
    }

    /// <summary>
    ///     关联标的提取为统一形态 [{"stock_id":"sz399975","name":"证券公司"}] ; 无标的返回 null
    /// </summary>
    private string? ReadStockListJson()
    {
        if (Stocks is not { Count: > 0 }) return null;
        var result = new List<Dictionary<string, string>>();
        foreach (var stock in Stocks)
        {
            var symbol = stock?["symbol"]?.ToString();
            if (string.IsNullOrEmpty(symbol)) continue;
            result.Add(new Dictionary<string, string>
            {
                ["stock_id"] = symbol,
                ["name"] = stock?["key"]?.ToString() ?? ""
            });
        }

        // 用 JsonUtil 序列化 : JsonNode.ToJsonString 会把中文转义成 \uXXXX , 与已入库 JSON 的风格不一致
        return result.Count > 0 ? JsonUtil.GetJson(result) : null;
    }

    /// <summary>
    ///     ext 是 JSON 字符串 , 内含 stocks 数组
    /// </summary>
    private static JsonArray? ReadStocks(JsonNode? node)
    {
        var extText = node?.ToString();
        if (string.IsNullOrEmpty(extText)) return null;
        try
        {
            return JsonNode.Parse(extText)?["stocks"] as JsonArray;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    /// <summary>
    ///     【标题】正文 → 取标题 ; 否则用正文前若干字兜底
    /// </summary>
    private static string ExtractTitle(string richText)
    {
        var text = richText.Trim();
        if (text.StartsWith('【'))
        {
            var end = text.IndexOf('】');
            if (end > 1) return text[1..end];
        }

        return text.Length <= TitleMaxLength ? text : text[..TitleMaxLength];
    }

    private static List<string> ReadTagNames(JsonNode? node)
    {
        if (node is not JsonArray tagArray) return [];
        return tagArray.Select(tag => tag?["name"]?.ToString() ?? "").Where(name => name != "").ToList();
    }
}
