using System.Text.Json.Nodes;
using KSpider.Json;
using KSpider.Model;
using KSpider.Spider.News;
using KSpider.Time;
using KSpider.Tool.Http;

namespace KSpider.Spider.Jin10News.Model;

/// <summary>
///     金十数据快讯的单条数据 ( 列表即全文 , 无按 id 重拉的接口 )
/// </summary>
public class Jin10FlashItem
{
    /// <summary>
    ///     无标题快讯用正文兜底的截断长度
    /// </summary>
    private const int TitleMaxLength = 60;

    /// <summary>
    ///     接口没有摘要字段 , 用正文截断作为列表摘要 ( 供消费端预览 )
    /// </summary>
    private const int SummaryMaxLength = 200;

    /// <summary>
    ///     id 是时间戳风格的字符串 ( 如 20260919105910403800 ) , 保持字符串形态
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    ///     发布时间 ( 北京时间字符串 , 无时区标记 )
    /// </summary>
    public string Time { get; set; } = "";

    /// <summary>
    ///     条目类型 : 0 普通快讯 , 2 图文 / 热榜类 ( 带 title 与 link )
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    ///     1 = 重要快讯
    /// </summary>
    public int Important { get; set; }

    public string Content { get; set; } = "";

    public string Title { get; set; } = "";

    /// <summary>
    ///     PLUS 专享条目的标题 ; 这类条目 content 为空且 lock=true ( 实测约占 20% )
    /// </summary>
    public string VipTitle { get; set; } = "";

    public string Pic { get; set; } = "";

    public List<string> Tags { get; set; } = [];

    public string NewsUrl => string.Format(Jin10NewsResource.DetailUrlTemplate, Id);

    public DateTime NewsTime => TimeTools.GetDateByTimeStrForFormat(Time, TimeTools.TimeFormatForStrikethrough);

    /// <summary>
    ///     PLUS 专享条目正文为空 ( 需付费账号 ) , 用 vip_title 兜底 , 至少保留标题信息 ;
    ///     是否属于专享条目可从原始 JSON 的 data.lock / data.exclusive_to 判断
    /// </summary>
    private string EffectiveContent => string.IsNullOrEmpty(Content) ? VipTitle : Content;

    private string DisplayTitle => !string.IsNullOrEmpty(Title) ? Title : ExtractTitle(EffectiveContent);

    public static Jin10FlashItem FromJson(JsonNode node)
    {
        return new Jin10FlashItem
        {
            Id = node["id"]?.ToString() ?? "",
            Time = node["time"]?.ToString() ?? "",
            Type = int.TryParse(node["type"]?.ToString(), out var type) ? type : 0,
            Important = int.TryParse(node["important"]?.ToString(), out var important) ? important : 0,
            Content = node["data"]?["content"]?.ToString() ?? "",
            Title = node["data"]?["title"]?.ToString() ?? "",
            VipTitle = node["data"]?["vip_title"]?.ToString() ?? "",
            Pic = node["data"]?["pic"]?.ToString() ?? "",
            Tags = ReadStringArray(node["tags"])
        };
    }

    public SpiderNewsListModel ToSpiderNewListModel()
    {
        return new SpiderNewsListModel
        {
            FromMedia = (int)FromTypeOfNews.Jin10Media,
            NewsUrl = NewsUrl,
            NewsTitle = DisplayTitle,
            NewsSummary = Truncate(EffectiveContent, SummaryMaxLength),
            NewsFrom = Jin10NewsResource.NewsFromName,
            NewsTime = NewsTime,
            NewsDownloadTime = DateTime.Now,
            Category = Jin10NewsResource.FlashCategoryNumber
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
        foreach (var line in EffectiveContent.Split('\n',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            segments.Add(new NewsContentSegment
            {
                TagType = "P",
                Value = line,
                ValueType = NewsContentSegment.TextType
            });
        if (!string.IsNullOrEmpty(Pic))
            segments.Add(new NewsContentSegment
            {
                TagType = "P",
                Value = "",
                ValueType = NewsContentSegment.ImgType,
                ResourceUri = Pic
            });

        return new NewsContentParseResult
        {
            Content = new SpiderNewsContentModel
            {
                NewsUrl = NewsUrl,
                NewsTitle = DisplayTitle,
                NewsSummary = Truncate(EffectiveContent, SummaryMaxLength),
                NewsFrom = Jin10NewsResource.NewsFromName,
                NewsTime = NewsTime,
                NewsKeyword = string.Join(",", Tags),
                NewsContentJson = JsonUtil.GetJson(segments),
                NewsContentText = string.Join("\n", segments
                    .Where(item => item.ValueType == NewsContentSegment.TextType)
                    .Select(item => item.Value))
            },
            Images = string.IsNullOrEmpty(Pic)
                ? []
                : [
                    new SpiderNewsImageListModel
                    {
                        NewsUrl = NewsUrl,
                        ImageResourceUrl = Pic,
                        ImageName = GetImageName(Pic)
                    }
                ]
        };
    }

    /// <summary>
    ///     金十图片地址带尺寸后缀 ( 实测形态 : .../demo.png/lite ) , 直接取最后一段会得到 "lite" ;
    ///     这里取最后一个像文件名的路径段 , 找不到再退回最后一段
    /// </summary>
    private static string GetImageName(string imageUrl)
    {
        var lastPath = HttpUrlTools.GetUrlLastPath(imageUrl);
        if (lastPath.Contains('.')) return lastPath;
        var segments = imageUrl.Split('?', 2)[0].Split('/');
        return segments.LastOrDefault(segment => segment.Contains('.')) ?? lastPath;
    }

    /// <summary>
    ///     【标题】正文 → 取标题 ; 否则用正文前若干字兜底
    /// </summary>
    private static string ExtractTitle(string content)
    {
        var text = content.Trim();
        if (text.StartsWith('【'))
        {
            var end = text.IndexOf('】');
            if (end > 1) return text[1..end];
        }

        return Truncate(text, TitleMaxLength);
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
