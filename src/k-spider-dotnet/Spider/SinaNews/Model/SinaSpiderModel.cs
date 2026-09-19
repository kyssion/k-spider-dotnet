using System.Text.Json.Nodes;
using KSpider.Json;
using KSpider.Model;
using KSpider.Spider.News;
using KSpider.Time;

namespace KSpider.Spider.SinaNews.Model;

/// <summary>
///     新浪 7x24 直播的单条快讯 ( 列表即全文 , 无单条接口 )
/// </summary>
public class SinaLiveItem
{
    /// <summary>
    ///     无标题快讯用正文兜底的截断长度
    /// </summary>
    private const int TitleMaxLength = 60;

    /// <summary>
    ///     接口没有摘要字段 , 用正文截断作为列表摘要 ( 供消费端预览 )
    /// </summary>
    private const int SummaryMaxLength = 200;

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
            Tags = ReadTagNames(node["tag"])
        };
    }

    public SpiderNewsListModel ToSpiderNewListModel()
    {
        return new SpiderNewsListModel
        {
            FromMedia = (int)FromTypeOfNews.SinaMedia,
            NewsUrl = NewsUrl,
            NewsTitle = DisplayTitle,
            NewsSummary = Truncate(RichText, SummaryMaxLength),
            NewsFrom = SinaNewsResource.NewsFromName,
            NewsTime = NewsTime,
            NewsDownloadTime = DateTime.Now,
            Category = SinaNewsResource.LiveCategoryNumber
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
        // 正文是纯文本 , 按换行拆段 ( 实测多为一整段 , 拆段属于防御性处理 )
        foreach (var line in RichText.Split('\n',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            segments.Add(new NewsContentSegment
            {
                TagType = "P",
                Value = line,
                ValueType = NewsContentSegment.TextType
            });

        return new NewsContentParseResult
        {
            Content = new SpiderNewsContentModel
            {
                NewsUrl = NewsUrl,
                NewsTitle = DisplayTitle,
                NewsSummary = Truncate(RichText, SummaryMaxLength),
                NewsFrom = SinaNewsResource.NewsFromName,
                NewsTime = NewsTime,
                NewsKeyword = string.Join(",", Tags),
                NewsContentJson = JsonUtil.GetJson(segments),
                NewsContentText = string.Join("\n", segments
                    .Where(item => item.ValueType == NewsContentSegment.TextType)
                    .Select(item => item.Value))
            }
            // 新浪快讯正文是纯文本 , 图片在 multimedia 字段里 ( 实测 100 条仅 1 条非空 ) , v1 不解析
        };
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

        return Truncate(text, TitleMaxLength);
    }

    private static List<string> ReadTagNames(JsonNode? node)
    {
        if (node is not JsonArray tagArray) return [];
        return tagArray.Select(tag => tag?["name"]?.ToString() ?? "").Where(name => name != "").ToList();
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
