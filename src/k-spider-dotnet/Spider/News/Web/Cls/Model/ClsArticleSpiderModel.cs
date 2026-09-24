using System.Text.Json.Nodes;
using KSpider.Common.Json;
using KSpider.Common.Time;
using KSpider.Model;
using KSpider.Spider.News.Flash.Cls;
using KSpider.Tool.Http;

namespace KSpider.Spider.News.Web.Cls.Model;

/// <summary>
///     频道文章列表接口的单条数据 ( /v3/depth/list ) , 列表只有摘要 , 正文在详情页 ——
///     与电报 ( 列表即全文 ) 不同 , 走网页型三段管线
/// </summary>
public class ClsArticleListItem
{
    /// <summary>
    ///     电报/文章共用的部分 : 无标题时用摘要兜底的截断长度
    /// </summary>
    private const int BriefTitleMaxLength = 60;

    /// <summary>
    ///     接口 ctime 为 unix 秒 ( 北京时间 ) , 固定按东八区换算 , 不依赖宿主时区
    /// </summary>
    private static readonly TimeSpan ChinaOffset = TimeSpan.FromHours(8);

    public long Id { get; set; }

    public long Ctime { get; set; }

    public string Title { get; set; } = "";

    public string Brief { get; set; } = "";

    public string Source { get; set; } = "";

    /// <summary>
    ///     广告标记 , 站点自带 ( 列表接口比东财多给的一步过滤依据 )
    /// </summary>
    public int IsAd { get; set; }

    /// <summary>
    ///     非空时该条目跳转站外 ( 没有 /detail/{id} 详情页 ) , 无法走三段管线
    /// </summary>
    public string ExternalLink { get; set; } = "";

    public List<string> TagNames { get; set; } = [];

    public string NewsUrl => string.Format(ClsArticleResource.DetailUrlTemplate, Id);

    public DateTime NewsTime => DateTimeOffset.FromUnixTimeSeconds(Ctime).ToOffset(ChinaOffset).DateTime;

    /// <summary>
    ///     广告与站外跳转条目不入库
    /// </summary>
    public bool ShouldSkip => IsAd == 1 || !string.IsNullOrEmpty(ExternalLink);

    public static ClsArticleListItem FromJson(JsonNode node)
    {
        return new ClsArticleListItem
        {
            Id = ReadLong(node["id"]),
            Ctime = ReadLong(node["ctime"]),
            Title = node["title"]?.ToString() ?? "",
            Brief = node["brief"]?.ToString() ?? "",
            Source = node["source"]?.ToString() ?? "",
            IsAd = (int)(node["is_ad"] ?? 0),
            ExternalLink = node["external_link"]?.ToString() ?? "",
            TagNames = ReadTagNames(node["article_tag"])
        };
    }

    public SpiderNewsListModel ToSpiderNewListModel(int categoryNumber)
    {
        return new SpiderNewsListModel
        {
            FromMedia = (int)FromTypeOfNews.ClsArticleMedia,
            NewsUrl = NewsUrl,
            NewsTitle = string.IsNullOrEmpty(Title) ? Truncate(Brief, BriefTitleMaxLength) : Title,
            NewsSummary = Brief,
            // source 是记者/编辑名 , 投稿 / 转载类条目可能为空 , 回退平台名 ( 与电报同款兜底 )
            NewsFrom = string.IsNullOrEmpty(Source) ? ClsNewsResource.NewsFromName : Source,
            NewsTime = NewsTime,
            NewsDownloadTime = DateTime.Now,
            Category = categoryNumber
        };
    }

    private static long ReadLong(JsonNode? node)
    {
        return long.TryParse(node?.ToString(), out var value) ? value : 0;
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

/// <summary>
///     详情页 __NEXT_DATA__ 里的 articleDetail ( 详情页为服务端渲染 , 正文 HTML 直接内嵌 )
/// </summary>
public class ClsArticleDetailInfo
{
    private static readonly TimeSpan ChinaOffset = TimeSpan.FromHours(8);

    public long Id { get; set; }

    public long Ctime { get; set; }

    public string Title { get; set; } = "";

    public string Brief { get; set; } = "";

    /// <summary>
    ///     正文 HTML 片段 ( 实测只出现 p / strong / img / h 等简单标签 )
    /// </summary>
    public string Content { get; set; } = "";

    public string AuthorName { get; set; } = "";

    public List<string> TagNames { get; set; } = [];

    /// <summary>
    ///     封面图 ( 正文之外 , 不在正文里时补进图片列表 )
    /// </summary>
    public List<string> Images { get; set; } = [];

    public DateTime NewsTime => DateTimeOffset.FromUnixTimeSeconds(Ctime).ToOffset(ChinaOffset).DateTime;

    public static ClsArticleDetailInfo FromJson(JsonNode articleDetail)
    {
        return new ClsArticleDetailInfo
        {
            Id = long.TryParse(articleDetail["id"]?.ToString(), out var id) ? id : 0,
            Ctime = long.TryParse(articleDetail["ctime"]?.ToString(), out var ctime) ? ctime : 0,
            Title = articleDetail["title"]?.ToString() ?? "",
            Brief = articleDetail["brief"]?.ToString() ?? "",
            Content = articleDetail["content"]?.ToString() ?? "",
            AuthorName = articleDetail["author"]?["name"]?.ToString() ?? "",
            TagNames = ReadTagNames(articleDetail["visibleTags"]),
            Images = ReadImages(articleDetail["images"])
        };
    }

    public SpiderNewsContentModel ToSpiderNewsContentModel(string newsUrl, List<NewsContentSegment> segments,
        string contentText)
    {
        return new SpiderNewsContentModel
        {
            NewsUrl = newsUrl,
            NewsTitle = Title,
            NewsSummary = Brief,
            NewsFrom = string.IsNullOrEmpty(AuthorName) ? ClsNewsResource.NewsFromName : AuthorName,
            NewsTime = NewsTime,
            NewsKeyword = TagNames.Count > 0 ? string.Join(",", TagNames) : null,
            NewsContentJson = JsonUtil.GetJson(segments),
            NewsContentText = contentText
        };
    }

    private static List<string> ReadTagNames(JsonNode? node)
    {
        if (node is not JsonArray tagArray) return [];
        return tagArray.Select(tag => tag?["name"]?.ToString() ?? "").Where(name => name != "").ToList();
    }

    private static List<string> ReadImages(JsonNode? node)
    {
        if (node is not JsonArray imageArray) return [];
        return imageArray.Select(image => image?.ToString() ?? "").Where(url => url != "").ToList();
    }
}
