using System.Text.Json.Nodes;
using KSpider.Json;
using KSpider.Model;
using KSpider.Spider.News;
using KSpider.Tool.Http;

namespace KSpider.Spider.WscnNews.Model;

/// <summary>
///     华尔街见闻 live 的单条快讯 ( 列表即全文 , 无单条接口 )
/// </summary>
public class WscnLiveItem
{
    /// <summary>
    ///     接口没有摘要字段 , 用正文截断作为列表摘要 ( 供消费端预览 )
    /// </summary>
    private const int SummaryMaxLength = 200;

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

    public List<string> Images { get; set; } = [];

    public List<string> Tags { get; set; } = [];

    public List<string> Channels { get; set; } = [];

    public string NewsUrl => string.IsNullOrEmpty(Uri)
        ? string.Format(WscnNewsResource.FallbackNewsUrlTemplate, Id)
        : Uri;

    public DateTime NewsTime => DateTimeOffset.FromUnixTimeSeconds(DisplayTime).ToOffset(ChinaOffset).DateTime;

    private string DisplayTitle => string.IsNullOrEmpty(Title) ? Truncate(ContentText, 60) : Title;

    public static WscnLiveItem FromJson(JsonNode node)
    {
        return new WscnLiveItem
        {
            Id = long.TryParse(node["id"]?.ToString(), out var id) ? id : 0,
            Title = node["title"]?.ToString() ?? "",
            ContentText = node["content_text"]?.ToString() ?? "",
            DisplayTime = long.TryParse(node["display_time"]?.ToString(), out var time) ? time : 0,
            Uri = node["uri"]?.ToString() ?? "",
            Images = ReadStringArray(node["images"]),
            Tags = ReadStringArray(node["tags"]),
            Channels = ReadStringArray(node["channels"])
        };
    }

    public SpiderNewsListModel ToSpiderNewListModel()
    {
        return new SpiderNewsListModel
        {
            FromMedia = (int)FromTypeOfNews.WscnMedia,
            NewsUrl = NewsUrl,
            NewsTitle = DisplayTitle,
            NewsSummary = Truncate(ContentText, SummaryMaxLength),
            NewsFrom = WscnNewsResource.NewsFromName,
            NewsTime = NewsTime,
            NewsDownloadTime = DateTime.Now,
            Category = WscnNewsResource.LiveCategoryNumber
        };
    }

    /// <summary>
    ///     快讯列表项本身即全文 , 原始内容直接取列表返回的条目 JSON
    /// </summary>
    public NewsContentOrigin ToContentOrigin(string itemJson)
    {
        return new NewsContentOrigin
        {
            NewsUrl = NewsUrl,
            OriginType = NewsContentOriginType.Json,
            NewsOriginContent = itemJson,
            Status = NewsContentOriginStatus.Success
        };
    }

    public NewsContentParseResult ToParseResult()
    {
        var segments = new List<NewsContentSegment>();
        foreach (var line in ContentText.Split('\n',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            segments.Add(new NewsContentSegment
            {
                TagType = "P",
                Value = line,
                ValueType = NewsContentSegment.TextType
            });
        foreach (var imageUrl in Images)
            segments.Add(new NewsContentSegment
            {
                TagType = "P",
                Value = "",
                ValueType = NewsContentSegment.ImgType,
                ResourceUri = imageUrl
            });

        return new NewsContentParseResult
        {
            Content = new SpiderNewsContentModel
            {
                NewsUrl = NewsUrl,
                NewsTitle = DisplayTitle,
                NewsSummary = Truncate(ContentText, SummaryMaxLength),
                NewsFrom = WscnNewsResource.NewsFromName,
                NewsTime = NewsTime,
                // 只用业务标签 ; 频道 channels 是内部英文 slug , 不放进关键字 ( 原始 JSON 里保留 )
                NewsKeyword = string.Join(",", Tags),
                NewsContentJson = JsonUtil.GetJson(segments),
                NewsContentText = string.Join("\n", segments
                    .Where(item => item.ValueType == NewsContentSegment.TextType)
                    .Select(item => item.Value))
            },
            Images = Images.Select(imageUrl => new SpiderNewsImageListModel
            {
                NewsUrl = NewsUrl,
                ImageResourceUrl = imageUrl,
                ImageName = HttpUrlTools.GetUrlLastPath(imageUrl)
            }).ToList()
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
