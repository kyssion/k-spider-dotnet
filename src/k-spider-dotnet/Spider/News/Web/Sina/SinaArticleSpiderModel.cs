using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.Json.Nodes;
using HtmlAgilityPack;
using KSpider.Model;
using KSpider.Spider.News.Flash.Sina;
using KSpider.Spider.News.Web;

namespace KSpider.Spider.News.Web.Sina;

/// <summary>
///     财经滚动接口 ( feed.mix ) 的单条列表数据。
///     字段与 ClsArticleListItem 同款约定 : 覆盖 spider_news_list 列表阶段要写的全部业务字段 ,
///     在 FromJson 一次性显式赋值 ( ToSpiderNewListModel 只做 1:1 映射 )。
/// </summary>
public class SinaRollListItem
{
    /// <summary>无标题时用导语兜底的截断长度 ( 与财联社列表同款 )</summary>
    private const int BriefTitleMaxLength = 60;

    /// <summary>接口 ctime 为 unix 秒 ( 北京时间 ) , 固定按东八区换算 , 不依赖宿主时区</summary>
    private static readonly TimeSpan ChinaOffset = TimeSpan.FromHours(8);

    public string NewsUrl { get; set; } = "";

    public string NewsTitle { get; set; } = "";

    public string NewsSummary { get; set; } = "";

    /// <summary>来源 : media_name 为投稿/转载媒体名 , 可能缺失 , 回退平台名</summary>
    public string NewsFrom { get; set; } = "";

    public DateTime NewsTime { get; set; }

    public DateTime NewsDownloadTime { get; set; }

    public FromTypeOfNews FromMedia { get; set; }

    public int Category { get; set; }

    /// <summary>
    ///     URL 缺失 / 不是文章页 ( 不含 doc- 标识 ) / ctime 缺失 , 不入库
    /// </summary>
    public bool ShouldSkip { get; set; }

    public static SinaRollListItem FromJson(JsonNode node, int categoryNumber)
    {
        var url = node["url"]?.ToString() ?? "";
        var ctime = long.TryParse(node["ctime"]?.ToString(), out var ctimeValue) ? ctimeValue : 0;
        var title = node["title"]?.ToString() ?? "";
        // 实测 summary 恒为空串 , 导语在 intro 字段 ; 仍按"intro 优先、summary 兜底"取
        var intro = node["intro"]?.ToString() ?? "";
        var brief = string.IsNullOrEmpty(intro) ? node["summary"]?.ToString() ?? "" : intro;

        return new SinaRollListItem
        {
            NewsUrl = url,
            NewsTitle = string.IsNullOrEmpty(title) ? Truncate(brief, BriefTitleMaxLength) : title,
            NewsSummary = brief,
            NewsFrom = ReadMediaName(node),
            NewsTime = DateTimeOffset.FromUnixTimeSeconds(ctime).ToOffset(ChinaOffset).DateTime,
            NewsDownloadTime = DateTime.Now,
            FromMedia = FromTypeOfNews.SinaMedia,
            Category = categoryNumber,
            ShouldSkip = string.IsNullOrEmpty(url) || !url.Contains("doc-", StringComparison.Ordinal) || ctime <= 0
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
            Category = Category
        };
    }

    // media_name 为空串时回退平台名 ( 与 7x24 快讯源同款 )
    private static string ReadMediaName(JsonNode node)
    {
        var mediaName = node["media_name"]?.ToString() ?? "";
        return string.IsNullOrEmpty(mediaName) ? SinaNewsResource.NewsFromName : mediaName;
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}

/// <summary>
///     栏目滚动页 ( roll/c/{cid}.shtml ) 列表 ul#listcontent 里的单个 li 条目。
///     页面时间只有 "(09月28日 22:15)" ( 无年份 ) , 年份从条目 URL 路径 /yyyy-MM-dd/doc- 补全。
/// </summary>
public partial class SinaColumnPageItem
{
    /// <summary>列表时间格式 : ( 09月28日 22:15 )</summary>
    [GeneratedRegex(@"\((\d{2})月(\d{2})日\s(\d{2}):(\d{2})\)")]
    private static partial Regex PageTimeRegex();

    /// <summary>条目 URL 里的发布日期路径 : /2026-09-28/doc- ( 栏目页条目都带 , 是年份唯一来源 )</summary>
    [GeneratedRegex(@"/(\d{4})-(\d{2})-(\d{2})/doc-")]
    private static partial Regex UrlDateRegex();

    public string NewsUrl { get; set; } = "";

    public string NewsTitle { get; set; } = "";

    public string NewsSummary { get; set; } = "";

    /// <summary>列表页没有来源字段 , 统一平台名 , 详情解析阶段再按 media 覆盖</summary>
    public string NewsFrom { get; set; } = "";

    public DateTime NewsTime { get; set; }

    public DateTime NewsDownloadTime { get; set; }

    public FromTypeOfNews FromMedia { get; set; }

    public int Category { get; set; }

    /// <summary>URL/标题缺失、不是文章页、或时间无法补全 ( URL 无日期路径 / 时间括号不匹配 ) , 不入库</summary>
    public bool ShouldSkip { get; set; }

    public static SinaColumnPageItem FromHtmlNode(HtmlNode liNode, int categoryNumber)
    {
        var anchor = liNode.SelectSingleNode("a");
        var timeSpan = liNode.SelectSingleNode("span");
        var url = anchor?.Attributes["href"]?.Value ?? "";
        var title = anchor?.InnerText.Trim() ?? "";
        var timeValid = false;
        var newsTime = DateTime.MinValue;

        // 年份取 URL 日期路径 , 月日与时刻取页面时间括号 ( 两者月日一致 , 时刻以页面为准 )
        var urlDate = UrlDateRegex().Match(url);
        var pageTime = PageTimeRegex().Match(timeSpan?.InnerText ?? "");
        if (urlDate.Success && pageTime.Success)
        {
            var timeText = $"{urlDate.Groups[1].Value}年{pageTime.Groups[1].Value}月{pageTime.Groups[2].Value}日 " +
                           $"{pageTime.Groups[3].Value}:{pageTime.Groups[4].Value}";
            timeValid = DateTime.TryParseExact(timeText, "yyyy年MM月dd日 HH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out newsTime);
        }

        return new SinaColumnPageItem
        {
            NewsUrl = url,
            NewsTitle = title,
            NewsSummary = "",
            NewsFrom = SinaNewsResource.NewsFromName,
            NewsTime = timeValid ? newsTime : DateTime.MinValue,
            NewsDownloadTime = DateTime.Now,
            FromMedia = FromTypeOfNews.SinaMedia,
            Category = categoryNumber,
            ShouldSkip = string.IsNullOrEmpty(url) || !url.Contains("doc-", StringComparison.Ordinal) ||
                         string.IsNullOrEmpty(title) || !timeValid
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
            Category = Category
        };
    }
}

/// <summary>
///     详情页 ( doc-*.shtml , 服务端渲染 ) 解析出的结构化信息。
///     头部节点 : h1.main-title 标题 / span.date 发布时间 / span.source 来源媒体 / div#artibody 正文 ,
///     meta 侧有 og:title、article:published_time 与 keywords。
/// </summary>
public class SinaArticleDetailInfo
{
    /// <summary>详情页发布时间格式 : 2026年09月28日 23:20</summary>
    private const string DetailTimeFormat = "yyyy年MM月dd日 HH:mm";

    /// <summary>页面标题后缀 , 取 title 兜底时剥掉</summary>
    private const string TitleSuffix = "_新浪财经_新浪网";

    public string Title { get; set; } = "";

    public string Brief { get; set; } = "";

    /// <summary>来源媒体名 , 为空时回退平台名 ( 见 ToSpiderNewsContentModel )</summary>
    public string MediaName { get; set; } = "";

    /// <summary>meta keywords 原文 ( 逗号分隔 ) , 落 news_keyword</summary>
    public string Keywords { get; set; } = "";

    public DateTime NewsTime { get; set; }

    /// <summary>正文 HTML 片段 ( div#artibody 内嵌的原始结构 )</summary>
    public string Content { get; set; } = "";

    public static SinaArticleDetailInfo FromHtmlDocument(HtmlDocument document)
    {
        var title = document.DocumentNode
            .SelectSingleNode("//meta[@property='og:title']")?.Attributes["content"]?.Value
            ?? document.DocumentNode.SelectSingleNode("//h1[@class='main-title']")?.InnerText.Trim()
            ?? document.DocumentNode.SelectSingleNode("//title")?.InnerText.Trim() ?? "";
        if (title.EndsWith(TitleSuffix, StringComparison.Ordinal)) title = title[..^TitleSuffix.Length];

        // 来源媒体 : 头部 .date-source 容器里的 span.source ( 部分模板 class 带 ent-source 前缀 )
        var mediaName = document.DocumentNode
            .SelectSingleNode("//div[contains(@class,'date-source')]/span[contains(@class,'source')]")?
            .InnerText.Trim() ?? "";

        // 发布时间 : 优先页面展示时间 , 缺失时回退 meta article:published_time ( ISO 8601 )
        var timeText = document.DocumentNode.SelectSingleNode("//span[@class='date']")?.InnerText.Trim() ?? "";
        DateTime newsTime;
        if (!DateTime.TryParseExact(timeText, DetailTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None,
                out newsTime))
        {
            var metaTime = document.DocumentNode
                .SelectSingleNode("//meta[@property='article:published_time']")?.Attributes["content"]?.Value ?? "";
            newsTime = DateTimeOffset.Parse(metaTime, CultureInfo.InvariantCulture).DateTime;
        }

        return new SinaArticleDetailInfo
        {
            Title = title,
            Brief = document.DocumentNode
                .SelectSingleNode("//meta[@property='og:description']")?.Attributes["content"]?.Value ?? "",
            MediaName = mediaName,
            Keywords = document.DocumentNode
                .SelectSingleNode("//meta[@name='keywords']")?.Attributes["content"]?.Value ?? "",
            NewsTime = newsTime,
            Content = document.DocumentNode.SelectSingleNode("//div[@id='artibody']")?.InnerHtml ?? ""
        };
    }

    public SpiderNewsContentModel ToSpiderNewsContentModel(string newsUrl, string contentJson, string contentText)
    {
        return new SpiderNewsContentModel
        {
            NewsUrl = newsUrl,
            NewsTitle = Title,
            NewsSummary = Brief,
            NewsFrom = string.IsNullOrEmpty(MediaName) ? SinaNewsResource.NewsFromName : MediaName,
            NewsTime = NewsTime,
            NewsKeyword = string.IsNullOrEmpty(Keywords) ? null : Keywords,
            NewsContentJson = contentJson,
            NewsContentText = contentText
        };
    }
}
