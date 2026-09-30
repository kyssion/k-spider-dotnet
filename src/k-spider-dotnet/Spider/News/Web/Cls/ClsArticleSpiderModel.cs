using System.Text.Json.Nodes;
using KSpider.Model;
using KSpider.Spider.News.Flash.Cls;
using KSpider.Spider.News.Web;
using KSpider.Common;

namespace KSpider.Spider.News.Web.Cls;

/// <summary>
///     频道文章列表接口的单条数据 ( /v3/depth/list ) , 列表只有摘要 , 正文在详情页 ——
///     与电报 ( 列表即全文 ) 不同 , 走网页型三段管线。
///     字段与 DfListInfo 同款约定 : 覆盖 spider_news_list 列表阶段要写的全部业务字段 ,
///     在 FromJson 一次性显式赋值 ( ToSpiderNewListModel 只做 1:1 映射 ) ;
///     站内内容 id 只在 FromJson 内部用于拼详情页地址 , 不作为属性存在 ,
///     数据库主键 / 时间戳列 / 状态机列 ( id / create_time / update_time / download_status_code / fail_count )
///     分别由数据库自增、触发器与 Job 层状态机维护 , 不在这里赋值。
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

    /// <summary>
    ///     详情页地址 , 作为 news_url 的稳定去重键 ; 条目 id 缺失时为空
    /// </summary>
    public string NewsUrl { get; set; } = "";

    /// <summary>
    ///     展示标题 : 无标题时用摘要截断兜底
    /// </summary>
    public string NewsTitle { get; set; } = "";

    public string NewsSummary { get; set; } = "";

    /// <summary>
    ///     来源 : source 是记者/编辑名 , 投稿/转载类条目可能为空 , 回退平台名 ( 与电报同款兜底 ) ;
    ///     品见条目署名在 author 字段 ( 如 "责编：若瑜" ) , 与 source 互斥出现 , 作第二优先级
    /// </summary>
    public string NewsFrom { get; set; } = "";

    /// <summary>
    ///     发布时间 ( 东八区 )
    /// </summary>
    public DateTime NewsTime { get; set; }

    /// <summary>
    ///     抓取时间 ( 列表响应解析完成时刻 )
    /// </summary>
    public DateTime NewsDownloadTime { get; set; }

    public FromTypeOfNews FromMedia { get; set; }

    /// <summary>
    ///     栏目分类号 , 由频道资源带入 ( 财联社文章 102-114 )
    /// </summary>
    public int Category { get; set; }

    /// <summary>
    ///     原始 ctime ( unix 秒 ) : 不落库 , 翻页游标取本页最老一条 ctime
    /// </summary>
    public long Ctime { get; set; }

    /// <summary>
    ///     广告 ( is_ad ) / 站外跳转 ( external_link 非空 , 没有 /detail/{id} 详情页 ) / 条目 id 缺失 , 不入库
    /// </summary>
    public bool ShouldSkip { get; set; }

    public static ClsArticleListItem FromJson(JsonNode node, int categoryNumber)
    {
        // 站内全局内容 id ( 电报与文章共用一套 ) , 只用于拼详情页地址 , 不落到任何属性上
        var articleId = ReadLong(node["id"]);
        var ctime = ReadLong(node["ctime"]);
        var title = node["title"]?.ToString() ?? "";
        var brief = node["brief"]?.ToString() ?? "";
        var source = node["source"]?.ToString() ?? "";
        if (string.IsNullOrEmpty(source)) source = node["author"]?.ToString() ?? "";

        return new ClsArticleListItem
        {
            NewsUrl = articleId > 0 ? string.Format(ClsArticleResource.DetailUrlTemplate, articleId) : "",
            NewsTitle = string.IsNullOrEmpty(title) ? Truncate(brief, BriefTitleMaxLength) : title,
            NewsSummary = brief,
            NewsFrom = string.IsNullOrEmpty(source) ? ClsNewsResource.NewsFromName : source,
            NewsTime = DateTimeOffset.FromUnixTimeSeconds(ctime).ToOffset(ChinaOffset).DateTime,
            NewsDownloadTime = DateTime.Now,
            FromMedia = FromTypeOfNews.ClsMedia,
            Category = categoryNumber,
            Ctime = ctime,
            ShouldSkip = (int)(node["is_ad"] ?? 0) == 1 || !string.IsNullOrEmpty(node["external_link"]?.ToString()) || articleId <= 0
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

    // 接口数值字段可能是数字或数字字符串 , 统一兜底解析
    private static long ReadLong(JsonNode? node)
    {
        return long.TryParse(node?.ToString(), out var value) ? value : 0;
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}

/// <summary>
///     详情页 __NEXT_DATA__ 里的 articleDetail ( 详情页为服务端渲染 , 正文 HTML 直接内嵌 )。
///     与列表条目同款约定 : 属性全部是解析时一次性落定的最终值
/// </summary>
public class ClsArticleDetailInfo
{
    private static readonly TimeSpan ChinaOffset = TimeSpan.FromHours(8);

    public string Title { get; set; } = "";

    public string Brief { get; set; } = "";

    /// <summary>
    ///     正文 HTML 片段 ( 实测只出现 p / strong / img / h 等简单标签 )
    /// </summary>
    public string Content { get; set; } = "";

    /// <summary>
    ///     作者 / 记者名 , 为空时回退平台名 ( 见 ToSpiderNewsContentModel )
    /// </summary>
    public string AuthorName { get; set; } = "";

    /// <summary>
    ///     文章标签列表 , 拼接后落 news_keyword
    /// </summary>
    public List<string> TagNames { get; set; } = [];

    /// <summary>
    ///     封面图 ( 正文之外 , 不在正文里时补进图片列表 )
    /// </summary>
    public List<string> Images { get; set; } = [];

    /// <summary>
    ///     发布时间 ( 东八区 )
    /// </summary>
    public DateTime NewsTime { get; set; }

    public static ClsArticleDetailInfo FromJson(JsonNode articleDetail)
    {
        var ctime = long.TryParse(articleDetail["ctime"]?.ToString(), out var ctimeValue) ? ctimeValue : 0;
        return new ClsArticleDetailInfo
        {
            Title = articleDetail["title"]?.ToString() ?? "",
            Brief = articleDetail["brief"]?.ToString() ?? "",
            Content = articleDetail["content"]?.ToString() ?? "",
            AuthorName = articleDetail["author"]?["name"]?.ToString() ?? "",
            TagNames = ReadTagNames(articleDetail["visibleTags"]),
            Images = ReadImages(articleDetail["images"]),
            NewsTime = DateTimeOffset.FromUnixTimeSeconds(ctime).ToOffset(ChinaOffset).DateTime
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
            NewsContentJson = JsonTools.GetJson(segments),
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
