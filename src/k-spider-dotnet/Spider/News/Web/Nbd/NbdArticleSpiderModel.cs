using System.Globalization;
using System.Text.RegularExpressions;
using KSpider.Model;
using KSpider.Spider.News.Web;

namespace KSpider.Spider.News.Web.Nbd;

/// <summary>
///     每经文章列表接口的单条数据 ( SSR 栏目页的文章链接 ) :
///     与 DfListInfo / ClsArticleListItem 同款约定 , 解析时一次性显式赋值最终值。
///     列表只有标题与日期 ( 链接路径自带完整 yyyy-MM-dd , 无时刻 ) , 正文在详情页 , 走网页型三段管线。
/// </summary>
public class NbdArticleListItem
{
    /// <summary>文章链接路径自带完整日期 ( 与同花顺"无年份需补全"不同 , 这里直接可用 )</summary>
    public string NewsUrl { get; set; } = "";

    public string NewsTitle { get; set; } = "";

    public string NewsFrom { get; set; } = "";

    public DateTime NewsTime { get; set; }

    public DateTime NewsDownloadTime { get; set; }

    public FromTypeOfNews FromMedia { get; set; }

    public int Category { get; set; }

    /// <summary>日期路径 / 标题缺失的坏数据标记</summary>
    public bool ShouldSkip { get; set; }

    /// <summary>
    ///     从条目锚点构造 : href ( …/articles/{yyyy-MM-dd}/{id}.html ) + 标题 ;
    ///     列表无时刻 , 时间取链接日期当日 00:00 ( 精确时刻由详情解析补齐 )
    /// </summary>
    public static NbdArticleListItem FromAnchor(string? href, string? title, int categoryNumber)
    {
        href = href?.Trim() ?? "";
        title = title?.Trim() ?? "";
        var dateMatch = DatePathRegex.Match(href);
        var valid = dateMatch.Success && !string.IsNullOrWhiteSpace(title);
        var time = DateTime.MinValue;
        if (dateMatch.Success)
            time = DateTime.TryParseExact(dateMatch.Groups[1].Value, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : DateTime.MinValue;

        return new NbdArticleListItem
        {
            NewsUrl = valid ? href : "",
            NewsTitle = title,
            NewsFrom = "每日经济新闻",
            NewsTime = valid && time > DateTime.MinValue ? time : DateTime.MinValue,
            NewsDownloadTime = DateTime.Now,
            FromMedia = FromTypeOfNews.NbdMedia,
            Category = categoryNumber,
            ShouldSkip = !valid || time == DateTime.MinValue
        };
    }

    public SpiderNewsListModel ToSpiderNewListModel()
    {
        return new SpiderNewsListModel
        {
            FromMedia = (int)FromMedia,
            NewsUrl = NewsUrl,
            NewsTitle = NewsTitle,
            NewsFrom = NewsFrom,
            NewsTime = NewsTime,
            NewsDownloadTime = NewsDownloadTime,
            Category = Category
        };
    }

    private static readonly Regex DatePathRegex =
        new(@"nbd\.com\.cn/articles/(\d{4}-\d{2}-\d{2})/\d+\.html", RegexOptions.Compiled);
}
