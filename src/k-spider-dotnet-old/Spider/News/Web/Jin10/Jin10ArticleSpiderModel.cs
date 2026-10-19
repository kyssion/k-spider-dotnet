using System.Text.Json.Nodes;
using KSpider.Common;
using KSpider.Model;
using KSpider.Spider.News.Web;

namespace KSpider.Spider.News.Web.Jin10;

/// <summary>
///     文章列表接口 ( reference?nav_bar_id= ) 的单条列表数据。
///     与各源列表项同款约定 : 覆盖 spider_news_list 列表阶段要写的全部业务字段 ,
///     在 FromJson 一次性显式赋值 ( ToSpiderNewListModel 只做 1:1 映射 )。
/// </summary>
public class Jin10ArticleListItem
{
    /// <summary>无标题时用导语兜底的截断长度 ( 与财联社/新浪/见闻列表同款 )</summary>
    private const int BriefTitleMaxLength = 60;

    public string NewsUrl { get; set; } = "";

    public string NewsTitle { get; set; } = "";

    public string NewsSummary { get; set; } = "";

    /// <summary>来源 : 优先作者昵称 ( 金十有专职作者 ) , 缺失回退平台名</summary>
    public string NewsFrom { get; set; } = "";

    public DateTime NewsTime { get; set; }

    public DateTime NewsDownloadTime { get; set; }

    public FromTypeOfNews FromMedia { get; set; }

    public int Category { get; set; }

    /// <summary>付费/会员专享 ( vip/super_vip/elite_vip 任一标记 )。付费条目在列表层就被跳过 , 正常入库行恒为 false</summary>
    public bool IsPaid { get; set; }

    /// <summary>
    ///     id 缺失、标题与导语全空、非文章形态 ( type != news , 如视频/音频卡片 ) ,
    ///     或付费专享条目 ( 匿名请求详情时 content 整体为空 , 无正文可解析 , 先不接入 ) , 不入库
    /// </summary>
    public bool ShouldSkip { get; set; }

    public static Jin10ArticleListItem FromJson(JsonNode node, int categoryNumber)
    {
        var id = long.TryParse(node["id"]?.ToString(), out var idValue) ? idValue : 0;
        var title = node["title"]?.ToString() ?? "";
        var brief = node["introduction"]?.ToString() ?? "";
        // 列表与详情的时间同格式 ( yyyy-MM-dd HH:mm:ss ) ; 严格解析 , 格式不匹配抛 FormatException ( 与东财同款 )
        var newsTime = TimeTools.GetDateByTimeStrForFormat(node["display_datetime"]?.ToString() ?? "",
            TimeTools.TimeFormatForStrikethrough);
        var isPaid = ReadVipFlag(node);

        return new Jin10ArticleListItem
        {
            NewsUrl = string.Format(Jin10ArticleResource.DetailPageUrlTemplate, id),
            NewsTitle = string.IsNullOrEmpty(title) ? Truncate(brief, BriefTitleMaxLength) : title,
            NewsSummary = brief,
            NewsFrom = ReadFromName(node),
            NewsTime = newsTime,
            NewsDownloadTime = DateTime.Now,
            FromMedia = FromTypeOfNews.Jin10Media,
            Category = categoryNumber,
            IsPaid = isPaid,
            ShouldSkip = id <= 0 || (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(brief)) ||
                         node["type"]?.ToString() != "news" || isPaid
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

    /// <summary>付费三标记 vip / super_vip / elite_vip 任一非零即付费专享</summary>
    public static bool ReadVipFlag(JsonNode node)
    {
        return ReadFlag(node, "vip") || ReadFlag(node, "super_vip") || ReadFlag(node, "elite_vip");
    }

    private static bool ReadFlag(JsonNode node, string name)
    {
        return int.TryParse(node[name]?.ToString(), out var value) && value != 0;
    }

    private static string ReadFromName(JsonNode node)
    {
        var author = node["author"]?["nick"]?.ToString();
        return string.IsNullOrEmpty(author) ? Jin10ArticleResource.NewsFromName : author;
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}

/// <summary>
///     文章详情接口 ( reference/getOne ) 解析出的信息。
///     正文在 content 字段 ( HTML 片段 ) , 时间 display_datetime 与列表同格式 ( yyyy-MM-dd HH:mm:ss )。
/// </summary>
public class Jin10ArticleDetailInfo
{
    public string Title { get; set; } = "";

    public string Brief { get; set; } = "";

    public string MediaName { get; set; } = "";

    public DateTime NewsTime { get; set; }

    /// <summary>正文 HTML 片段 ( 含图片 , 内嵌 video 标签解析时跳过 )</summary>
    public string Content { get; set; } = "";

    public static Jin10ArticleDetailInfo FromJson(JsonNode dataNode)
    {
        return new Jin10ArticleDetailInfo
        {
            Title = dataNode["title"]?.ToString() ?? "",
            Brief = dataNode["introduction"]?.ToString() ?? "",
            MediaName = ReadFromName(dataNode),
            NewsTime = TimeTools.GetDateByTimeStrForFormat(dataNode["display_datetime"]?.ToString() ?? "",
                TimeTools.TimeFormatForStrikethrough),
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
            NewsFrom = string.IsNullOrEmpty(MediaName) ? Jin10ArticleResource.NewsFromName : MediaName,
            NewsTime = NewsTime,
            NewsKeyword = null,
            NewsContentJson = contentJson,
            NewsContentText = contentText
        };
    }

    private static string ReadFromName(JsonNode dataNode)
    {
        var author = dataNode["author"]?["nick"]?.ToString();
        if (!string.IsNullOrEmpty(author)) return author;
        var nick = dataNode["user_nick"]?.ToString() ?? "";
        return string.IsNullOrEmpty(nick) ? Jin10ArticleResource.NewsFromName : nick;
    }
}
