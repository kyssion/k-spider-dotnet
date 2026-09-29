using System.Text.Json.Nodes;
using KSpider.Model;
using KSpider.Spider.News.Web;

namespace KSpider.Spider.News.Web.Wscn;

/// <summary>
///     文章列表接口 ( apiv1/content/articles ) 的单条列表数据。
///     与各源列表项同款约定 : 覆盖 spider_news_list 列表阶段要写的全部业务字段 ,
///     在 FromJson 一次性显式赋值 ( ToSpiderNewListModel 只做 1:1 映射 )。
/// </summary>
public class WscnArticleListItem
{
    /// <summary>无标题时用导语兜底的截断长度 ( 与财联社/新浪列表同款 )</summary>
    private const int BriefTitleMaxLength = 60;

    /// <summary>接口 display_time 为 unix 秒 ( 北京时间 ) , 固定按东八区换算 , 不依赖宿主时区</summary>
    private static readonly TimeSpan ChinaOffset = TimeSpan.FromHours(8);

    public string NewsUrl { get; set; } = "";

    public string NewsTitle { get; set; } = "";

    public string NewsSummary { get; set; } = "";

    /// <summary>来源 : 优先作者名 ( 见闻有专职作者 ) , 缺失回退源媒体名 , 再回退平台名</summary>
    public string NewsFrom { get; set; } = "";

    public DateTime NewsTime { get; set; }

    public DateTime NewsDownloadTime { get; set; }

    public FromTypeOfNews FromMedia { get; set; }

    public int Category { get; set; }

    /// <summary>付费/会员专享 ( 见闻 is_priced ) : 正文会被截断 , 落库标记 spider_news_list.is_paid</summary>
    public bool IsPaid { get; set; }

    /// <summary>uri / 时间缺失、标题与导语全空、或非文章布局 ( layout != wscn-layout , 如视频卡片 ) , 不入库</summary>
    public bool ShouldSkip { get; set; }

    public static WscnArticleListItem FromJson(JsonNode node)
    {
        // uri 规范化 : 付费条目带 "?layout=wscn-layout" 可变查询串 ( 如 /premium/articles/3782635?layout=... ) ,
        // 去重键必须用稳定形态 , 统一剥掉查询串
        var url = node["uri"]?.ToString() ?? "";
        var queryIndex = url.IndexOf('?');
        if (queryIndex >= 0) url = url[..queryIndex];
        var displayTime = long.TryParse(node["display_time"]?.ToString(), out var timeValue) ? timeValue : 0;
        var title = node["title"]?.ToString() ?? "";
        var brief = node["content_short"]?.ToString() ?? "";
        var categories = (node["categories"] as JsonArray)?
            .Where(category => category != null).Select(category => category!.ToString()).ToArray();

        return new WscnArticleListItem
        {
            NewsUrl = url,
            NewsTitle = string.IsNullOrEmpty(title) ? Truncate(brief, BriefTitleMaxLength) : title,
            NewsSummary = brief,
            NewsFrom = ReadFromName(node),
            NewsTime = DateTimeOffset.FromUnixTimeSeconds(displayTime).ToOffset(ChinaOffset).DateTime,
            NewsDownloadTime = DateTime.Now,
            FromMedia = FromTypeOfNews.WscnMedia,
            Category = WscnArticleResource.InferCategory(categories),
            IsPaid = node["is_priced"]?.GetValue<bool>() ?? false,
            ShouldSkip = string.IsNullOrEmpty(url) || displayTime <= 0 ||
                         (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(brief)) ||
                         node["layout"]?.ToString() != "wscn-layout"
        };
    }

    public SpiderNewsListModel ToSpiderNewListModel()
    {
        return new SpiderNewsListModel
        {
            FromMedia = (int)FromMedia,
            NewsUrl = NewsUrl,
            NewsTitle = NewsTitle,
            NewsSummary = NewsSummary,
            NewsFrom = NewsFrom,
            NewsTime = NewsTime,
            NewsDownloadTime = NewsDownloadTime,
            Category = Category,
            IsPaid = IsPaid
        };
    }

    // 见闻文章以专职作者署名 , 无作者时回退转载源名 , 再回退平台名
    private static string ReadFromName(JsonNode node)
    {
        var author = node["author"]?["display_name"]?.ToString();
        if (!string.IsNullOrEmpty(author)) return author;
        var sourceName = node["source_name"]?.ToString() ?? "";
        return string.IsNullOrEmpty(sourceName) ? WscnArticleResource.NewsFromName : sourceName;
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}

/// <summary>
///     文章详情接口 ( apiv1/content/articles/{id}?extract=0 ) 解析出的信息。
///     正文在 content 字段 ( HTML 片段 ) , 时间 display_time 为 unix 秒。
/// </summary>
public class WscnArticleDetailInfo
{
    public string Title { get; set; } = "";

    public string Brief { get; set; } = "";

    public string MediaName { get; set; } = "";

    public DateTime NewsTime { get; set; }

    /// <summary>正文 HTML 片段 ( extract=0 时含图片 )</summary>
    public string Content { get; set; } = "";

    public static WscnArticleDetailInfo FromJson(JsonNode dataNode)
    {
        var displayTime = long.TryParse(dataNode["display_time"]?.ToString(), out var timeValue) ? timeValue : 0;
        return new WscnArticleDetailInfo
        {
            Title = dataNode["title"]?.ToString() ?? "",
            Brief = dataNode["content_short"]?.ToString() ?? "",
            MediaName = ReadFromName(dataNode),
            NewsTime = DateTimeOffset.FromUnixTimeSeconds(displayTime)
                .ToOffset(TimeSpan.FromHours(8)).DateTime,
            Content = dataNode["content"]?.ToString() ?? ""
        };
    }

    public SpiderNewsContentModel ToSpiderNewsContentModel(string newsUrl, string contentJson, string contentText)
    {
        return new SpiderNewsContentModel
        {
            NewsUrl = newsUrl,
            NewsTitle = Title,
            NewsSummary = Brief,
            NewsFrom = string.IsNullOrEmpty(MediaName) ? WscnArticleResource.NewsFromName : MediaName,
            NewsTime = NewsTime,
            NewsKeyword = null,
            NewsContentJson = contentJson,
            NewsContentText = contentText
        };
    }

    private static string ReadFromName(JsonNode dataNode)
    {
        var author = dataNode["author"]?["display_name"]?.ToString();
        if (!string.IsNullOrEmpty(author)) return author;
        var sourceName = dataNode["source_name"]?.ToString() ?? "";
        return string.IsNullOrEmpty(sourceName) ? WscnArticleResource.NewsFromName : sourceName;
    }
}
