namespace KSpider.Spider.News.Web.Ths;

/// <summary>
///     同花顺财经文章频道常量与栏目定义 ( 2026-09-30 实测 ) :
///     栏目页是 SSR HTML 列表 ( 与快讯的 JSON 接口不同源 ) , **GBK 编码** ( 响应头 charset=gbk ,
///     HttpClient 按 charset 解码依赖 CodePagesEncodingProvider , 见 HttpClientTools 静态构造 ) ;
///     列表条目时间"MM月dd日 HH:mm"无年份 , 年份从链接路径 /{yyyymmdd}/c{id}.shtml 补全 ( 新浪先例 ) ;
///     翻页 index_{n}.shtml 页码 , 每页 25 条 ; 详情页为 UTF-8 SSR。
///     与快讯 ( Spider/News/Flash/Ths ) 共用 ThsMedia , 管线归属由注册表决定。
/// </summary>
public static class ThsArticleResource
{
    /// <summary>解析器标识 : 同花顺文章 SSR HTML → 结构化片段 ( origin 行路由用 )</summary>
    public const string ParserCode = "ths-article-v1";

    /// <summary>
    ///     栏目列表页模板 , {0} 为栏目代码 ( today_list 等 ) ; {1} 为页码后缀 ( 首页空串 , 翻页 index_{n} )
    /// </summary>
    public const string ListUrlTemplate = "https://news.10jqka.com.cn/{0}_list/{1}";

    public const string ResourceHost = "news.10jqka.com.cn";

    /// <summary>
    ///     每页条数 ( 服务端固定 25 , 无翻页参数 )
    /// </summary>
    public const int PageArticleCount = 25;

    /// <summary>
    ///     文章栏目分类号段 : 接在快讯 501 之后 ( 同花顺 501-599 段内 )
    /// </summary>
    public static readonly ThsArticleColumnResource[] ArticleColumnResourceList =
    {
        new() { ColumnCode = "today", ColumnName = "财经要闻", CategoryNumber = 502 },
        new() { ColumnCode = "cjzx", ColumnName = "宏观经济", CategoryNumber = 503 },
        new() { ColumnCode = "fssgsxw", ColumnName = "公司新闻", CategoryNumber = 504 },
        new() { ColumnCode = "cjkx", ColumnName = "产经新闻", CategoryNumber = 505 },
        new() { ColumnCode = "fortune", ColumnName = "财经评论", CategoryNumber = 506 },
        new() { ColumnCode = "jrsc", ColumnName = "金融市场", CategoryNumber = 507 },
        new() { ColumnCode = "guojicj", ColumnName = "国际财经", CategoryNumber = 508 },
        new() { ColumnCode = "region", ColumnName = "区域经济", CategoryNumber = 509 },
        new() { ColumnCode = "cjrw", ColumnName = "财经人物", CategoryNumber = 510 },
        new() { ColumnCode = "fc", ColumnName = "房产", CategoryNumber = 511 }
    };

    public struct ThsArticleColumnResource
    {
        /// <summary>栏目代码 ( 列表页 URL 的 {code}_list 部分 , 同时作为 NewsColumn 的 ColumnId )</summary>
        public string ColumnCode { get; set; }

        public string ColumnName { get; set; }

        public int CategoryNumber { get; set; }
    }
}
