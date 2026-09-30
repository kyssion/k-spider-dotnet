using System.Globalization;
using System.Text.RegularExpressions;
using KSpider.Model;
using KSpider.Spider.News.Flash.Ths;
using KSpider.Spider.News.Web;
using KSpider.Common;

namespace KSpider.Spider.News.Web.Ths;

/// <summary>
///     同花顺文章列表接口的单条数据 ( SSR 栏目页 list-con 区的 arc-title 条目 ) :
///     与 DfListInfo / ClsArticleListItem 同款约定 , 解析时一次性显式赋值最终值。
///     列表只有标题与时间 , 正文在详情页 , 走网页型三段管线。
/// </summary>
public class ThsArticleListItem
{
    private const int BriefTitleMaxLength = 60;

    /// <summary>列表条目时间"MM月dd日 HH:mm"无年份 , 年份取链接路径日期</summary>
    public string NewsUrl { get; set; } = "";

    public string NewsTitle { get; set; } = "";

    public string NewsFrom { get; set; } = "";

    public DateTime NewsTime { get; set; }

    public DateTime NewsDownloadTime { get; set; }

    public FromTypeOfNews FromMedia { get; set; }

    public int Category { get; set; }

    /// <summary>
    ///     站内 seq ( data-seq 属性 ) , 与快讯共用一套内容 id ; 详情地址路径日期缺失等坏数据标记
    /// </summary>
    public bool ShouldSkip { get; set; }

    /// <summary>
    ///     从条目 HTML 属性构造 : href ( http://news.10jqka.com.cn/{yyyymmdd}/c{seq}.shtml ) +
    ///     标题 + 时刻"MM月dd日 HH:mm" ; 年份从 href 的日期路径补全 ( 与新浪栏目页同款 )
    /// </summary>
    public static ThsArticleListItem FromAnchor(string href, string title, string timeText, int categoryNumber)
    {
        // href 日期路径 → 年份 ; 无日期路径的条目视为坏数据 ( 与新浪同款口径 )
        var dateMatch = RegexCache.DatePathRegex.Match(href ?? "");
        var seqMatch = RegexCache.SeqRegex.Match(href ?? "");
        var valid = dateMatch.Success && seqMatch.Success && !string.IsNullOrWhiteSpace(title);

        var time = DateTime.MinValue;
        if (valid)
        {
            var year = dateMatch.Groups[1].Value[..4];
            // timeText 形如 "09月30日 13:10" ; 缺时刻回退当日 00:00
            var full = $"{year}年{timeText}";
            time = DateTime.TryParseExact(full, "yyyy年MM月dd日 HH:mm", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed) ? parsed :
                (DateTime.TryParseExact($"{year}年{timeText}", "yyyy年MM月dd日", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dateOnly) ? dateOnly : DateTime.MinValue);
            valid &= time > DateTime.MinValue;
        }

        return new ThsArticleListItem
        {
            NewsUrl = valid ? $"https://news.10jqka.com.cn/{dateMatch.Groups[1].Value}/c{seqMatch.Groups[1].Value}.shtml" : "",
            NewsTitle = title.Trim(),
            NewsFrom = ThsNewsResource.NewsFromName,
            NewsTime = time,
            NewsDownloadTime = DateTime.Now,
            FromMedia = FromTypeOfNews.ThsMedia,
            Category = categoryNumber,
            ShouldSkip = !valid
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

    /// <summary>同花顺正文无摘要字段 , 空标题兜底截断长度备用</summary>
    public static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static class RegexCache
    {
        /// <summary>链接日期路径 : /20260930/c680400243.shtml 的 20260930</summary>
        public static readonly Regex DatePathRegex =
            new(@"news\.10jqka\.com\.cn/(\d{8})/c\d+\.shtml", RegexOptions.Compiled);

        public static readonly Regex SeqRegex = new(@"c(\d+)\.shtml", RegexOptions.Compiled);
    }
}
