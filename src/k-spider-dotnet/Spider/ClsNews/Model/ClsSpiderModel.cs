using System.Text.Json.Nodes;
using KSpider.Json;
using KSpider.Model;
using KSpider.Spider.News;
using KSpider.Tool.Http;

namespace KSpider.Spider.ClsNews.Model;

/// <summary>
///     电报列表接口的单条数据 ( 列表即全文 , 无详情接口 )
/// </summary>
public class ClsRollItem
{
    /// <summary>
    ///     电报约半数条目没有标题 , 用摘要兜底时的截断长度
    /// </summary>
    private const int BriefTitleMaxLength = 60;

    /// <summary>
    ///     接口 ctime 为 unix 秒 ( 北京时间 ) , 固定按东八区换算 , 不依赖宿主时区
    /// </summary>
    private static readonly TimeSpan ChinaOffset = TimeSpan.FromHours(8);

    public long Id { get; set; }

    public string Title { get; set; } = "";

    public string Brief { get; set; } = "";

    public string Content { get; set; } = "";

    /// <summary>
    ///     发布时间 , unix 秒
    /// </summary>
    public long Ctime { get; set; }

    public string CoverImage { get; set; } = "";

    public List<string> Images { get; set; } = [];

    public List<string> Subjects { get; set; } = [];

    public string NewsUrl => string.Format(ClsNewsResource.DetailUrlTemplate, Id);

    public DateTime NewsTime => DateTimeOffset.FromUnixTimeSeconds(Ctime).ToOffset(ChinaOffset).DateTime;

    /// <summary>
    ///     无标题电报用摘要兜底 , 保证标题不为空
    /// </summary>
    private string DisplayTitle => string.IsNullOrEmpty(Title) ? Truncate(Brief, BriefTitleMaxLength) : Title;

    public static ClsRollItem FromJson(JsonNode node)
    {
        return new ClsRollItem
        {
            Id = ReadLong(node["id"]),
            Title = node["title"]?.ToString() ?? "",
            Brief = node["brief"]?.ToString() ?? "",
            Content = node["content"]?.ToString() ?? "",
            Ctime = ReadLong(node["ctime"]),
            CoverImage = node["img"]?.ToString() ?? "",
            Images = ReadImageUrls(node["images"]),
            Subjects = ReadSubjectNames(node["subjects"])
        };
    }

    public SpiderNewsListModel ToSpiderNewListModel()
    {
        return new SpiderNewsListModel
        {
            FromMedia = (int)FromTypeOfNews.ClsMedia,
            NewsUrl = NewsUrl,
            NewsTitle = DisplayTitle,
            NewsSummary = Brief,
            NewsFrom = ClsNewsResource.NewsFromName,
            NewsTime = NewsTime,
            NewsDownloadTime = DateTime.Now,
            Category = ClsNewsResource.TelegraphCategoryNumber
            // 下载状态由列表任务按 "是否带内联原始内容" 统一置位 , 这里保持默认 0
        };
    }

    /// <summary>
    ///     电报列表项本身即全文 , 原始内容直接取列表返回的条目 JSON
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
        var imageUrls = AllImageUrls();
        var segments = new List<NewsContentSegment>();
        // 正文是纯文本 , 按换行拆段 ( 实测多数电报无换行 , 拆段属于防御性处理 )
        foreach (var line in Content.Split('\n',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            segments.Add(new NewsContentSegment
            {
                TagType = "P",
                Value = line,
                ValueType = NewsContentSegment.TextType
            });
        foreach (var imageUrl in imageUrls)
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
                NewsSummary = Brief,
                NewsFrom = ClsNewsResource.NewsFromName,
                NewsTime = NewsTime,
                NewsKeyword = string.Join(",", Subjects),
                NewsContentJson = JsonUtil.GetJson(segments),
                NewsContentText = string.Join("\n", segments
                    .Where(item => item.ValueType == NewsContentSegment.TextType)
                    .Select(item => item.Value))
            },
            Images = imageUrls.Select(imageUrl => new SpiderNewsImageListModel
            {
                NewsUrl = NewsUrl,
                ImageResourceUrl = imageUrl,
                ImageName = HttpUrlTools.GetUrlLastPath(imageUrl)
            }).ToList()
        };
    }

    /// <summary>
    ///     正文图片 + 封面图 ( 封面不在正文里时补上 ) , 保持出现顺序
    /// </summary>
    private List<string> AllImageUrls()
    {
        var imageUrls = new List<string>(Images);
        if (!string.IsNullOrEmpty(CoverImage) && !imageUrls.Contains(CoverImage)) imageUrls.Add(CoverImage);
        return imageUrls;
    }

    // 接口数值字段可能是数字或数字字符串 , 统一兜底解析
    private static long ReadLong(JsonNode? node)
    {
        return long.TryParse(node?.ToString(), out var value) ? value : 0;
    }

    private static List<string> ReadImageUrls(JsonNode? node)
    {
        if (node is not JsonArray imageArray) return [];
        return imageArray.Select(image => image?.ToString() ?? "").Where(url => url != "").ToList();
    }

    private static List<string> ReadSubjectNames(JsonNode? node)
    {
        if (node is not JsonArray subjectArray) return [];
        return subjectArray.Select(subject => subject?["subject_name"]?.ToString() ?? "")
            .Where(name => name != "").ToList();
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
