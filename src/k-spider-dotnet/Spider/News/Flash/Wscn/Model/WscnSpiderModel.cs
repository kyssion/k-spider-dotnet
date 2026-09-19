using System.Text.Json.Nodes;
using KSpider.Common.Json;
using KSpider.Model;
using KSpider.Spider.News.Flash;

namespace KSpider.Spider.News.Flash.Wscn.Model;

/// <summary>
///     华尔街见闻 live 的单条快讯 ( 列表即全文 ) , 直接映射为可入库的快讯记录
/// </summary>
public class WscnLiveItem
{
    /// <summary>
    ///     接口没有标题时用正文截断的长度
    /// </summary>
    private const int TitleMaxLength = 60;

    /// <summary>
    ///     接口 display_time 为 unix 秒 ( 北京时间 ) , 固定按东八区换算 , 不依赖宿主时区
    /// </summary>
    private static readonly TimeSpan ChinaOffset = TimeSpan.FromHours(8);

    public long Id { get; set; }

    public string Title { get; set; } = "";

    /// <summary>
    ///     纯文本正文 ( 接口同时给了 content 的 HTML 形态 , 这里用纯文本避免再解析一次 HTML )
    /// </summary>
    public string ContentText { get; set; } = "";

    /// <summary>
    ///     发布时间 , unix 秒
    /// </summary>
    public long DisplayTime { get; set; }

    public string Uri { get; set; } = "";

    /// <summary>
    ///     重要度标记 , 实测 score=2 的条目为重要新闻 ( 商务部回应等 )
    /// </summary>
    public int Score { get; set; }

    public List<string> Images { get; set; } = [];

    public List<string> Tags { get; set; } = [];

    public string NewsUrl => string.IsNullOrEmpty(Uri)
        ? string.Format(WscnNewsResource.FallbackNewsUrlTemplate, Id)
        : Uri;

    public DateTime NewsTime => DateTimeOffset.FromUnixTimeSeconds(DisplayTime).ToOffset(ChinaOffset).DateTime;

    private string DisplayTitle => string.IsNullOrEmpty(Title) ? Truncate(ContentText, TitleMaxLength) : Title;

    /// <summary>
    ///     重要度映射 : score 2→2 重要 , 其余→1 普通
    /// </summary>
    private short FlashLevel => (short)(Score >= 2 ? 2 : 1);

    public static WscnLiveItem FromJson(JsonNode node)
    {
        return new WscnLiveItem
        {
            Id = long.TryParse(node["id"]?.ToString(), out var id) ? id : 0,
            Title = node["title"]?.ToString() ?? "",
            ContentText = node["content_text"]?.ToString() ?? "",
            DisplayTime = long.TryParse(node["display_time"]?.ToString(), out var time) ? time : 0,
            Uri = node["uri"]?.ToString() ?? "",
            Score = int.TryParse(node["score"]?.ToString(), out var score) ? score : 1,
            Images = ReadStringArray(node["images"]),
            Tags = ReadStringArray(node["tags"])
        };
    }

    public SpiderFlashNewsModel ToFlashNewsModel(string itemJson)
    {
        return new SpiderFlashNewsModel
        {
            FromMedia = (int)FromTypeOfNews.WscnMedia,
            Category = WscnNewsResource.LiveCategoryNumber,
            NewsUrl = NewsUrl,
            NewsTime = NewsTime,
            Title = DisplayTitle,
            Content = ContentText,
            // 只用业务标签 ; 频道 channels 是内部英文 slug , 不放进关键字 ( 原始 JSON 里保留 )
            Keyword = string.Join(",", Tags),
            Level = FlashLevel,
            StockList = null,
            ImageUrls = Images.Count > 0 ? JsonUtil.GetJson(Images) : null,
            RawContent = itemJson
        };
    }

    private static List<string> ReadStringArray(JsonNode? node)
    {
        if (node is not JsonArray array) return [];
        return array.Select(item => item?.ToString() ?? "").Where(item => item != "").ToList();
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
