using KSpider.Spider.News.Web;

namespace KSpider.Spider.News.Web.Jin10;

/// <summary>
///     金十数据文章源 ( 网页抓取型三段 ) 常量 , 与快讯 ( Spider/News/Flash/Jin10 ) 是同一网站的两条管线 ,
///     共用 FromTypeOfNews.Jin10Media , 分别注册在 NewsSpiderRegistry 与 FlashNewsSpiderRegistry。
///     文章站是 xnews.jin10.com ( 「市场参考」, news.jin10.com 301 过去 ) , 列表与详情都是
///     reference-api.jin10.com 的 JSON 接口 ( 2026-09 实测 ) : 页码翻页、越界页返回空列表、
///     满页上限 100 ; 详情正文在 data.content 字段 ( HTML 片段 , 实测标签集
///     p/strong/span/h2/a/img/figure/figcaption/video/blockquote/div )。
/// </summary>
public static class Jin10ArticleResource
{
    /// <summary>解析器标识 : 金十详情 JSON data.content → 结构化片段 ( origin 行路由用 )</summary>
    public const string ParserCode = "jin10-article-v1";

    /// <summary>
    ///     文章列表接口 , {0}=nav_bar_id ( 栏目 ) , {1}=page 页码 ( 1 起 ) , {2}=page_size。
    ///     实测边界 : page_size 上限 100 , 超过返回显式 400 ( value must be inside range [1, 100] ) ;
    ///     越界页码返回空 list ( status 仍 200 ) , 空页即末页
    /// </summary>
    public const string ListUrlTemplate =
        "https://reference-api.jin10.com/reference?nav_bar_id={0}&page={1}&page_size={2}";

    /// <summary>文章详情接口 , {0} 为文章 id ( 列表条目 id )</summary>
    public const string DetailUrlTemplate = "https://reference-api.jin10.com/reference/getOne?id={0}&type=news";

    public const string ResourceHost = "reference-api.jin10.com";

    /// <summary>
    ///     列表接口的客户端标识头 ( 与 <see cref="Version" /> 一起写死 ) ; 详情接口是另一套
    ///     <see cref="DetailAppId" /> , 两套头不通用 ; 缺头直接 502。被拒时对照网页端请求更新
    /// </summary>
    public const string AppId = "irINJPgCgrndSp0F";

    /// <summary>详情接口 ( reference/getOne ) 的客户端标识头 , 与列表接口的 <see cref="AppId" /> 不同</summary>
    public const string DetailAppId = "arU9WZF7TC9m7nWn";

    public const string Version = "1.0.1";

    /// <summary>单页上限 : 超过时接口返回显式 400 ( 见 <see cref="ListUrlTemplate" /> ) , 请求侧钳制到 100</summary>
    public const int MaxPageSize = 100;

    public const string NewsFromName = "金十数据";

    /// <summary>
    ///     详情页规范形态 URL ( 与站点 sitemap 一致 ) , 也是列表行的去重键 ;
    ///     接口自带的 detail_url 是 webapp 页带查询串形态 , 不用作去重键
    /// </summary>
    public const string DetailPageUrlTemplate = "https://xnews.jin10.com/details/{0}";

    /// <summary>条目规范 URL 形如 https://xnews.jin10.com/details/231303 , 文章 id 取末段数字</summary>
    public const string ArticleIdPattern = @"/details/(\d+)";

    /// <summary>
    ///     文章源栏目定义 : ( 栏目 id = 接口 nav_bar_id , 栏目名 , 分类号 )。
    ///     栏目按编辑位划分且互有重叠 ( 综合流与热点头条重叠约九成 ) , 重复条目由 news_url 去重吸收 ;
    ///     综合流实测仍有独有内容 ( 早餐/财料栏目在综合流前 500 条命中不足一成 ) , 必须保留 ;
    ///     分类号段 402-406 接在金十快讯 401 之后 , 仍在金十 401-499 段内。
    ///     VIP 专区 ( nav_bar_id=77 ) 与精英专区 ( nav_bar_id=84 ) 未接 :
    ///     其条目匿名请求详情时 content 整体为空 ( 无正文可解析 ) , 待放开订阅态后再接
    /// </summary>
    public static readonly (string ColumnId, string ColumnName, int CategoryNumber)[] ArticleColumnList =
    {
        ("28", "综合", 402),
        ("30", "金十早餐", 403),
        ("31", "精选分析", 404),
        ("53", "热点头条", 405),
        ("58", "财料", 406)
    };
}
