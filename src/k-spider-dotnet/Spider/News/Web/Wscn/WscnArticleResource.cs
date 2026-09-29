using KSpider.Spider.News.Web;

namespace KSpider.Spider.News.Web.Wscn;

/// <summary>
///     华尔街见闻文章源 ( 网页抓取型三段 ) 常量 , 与 live 快讯 ( Spider/News/Flash/Wscn ) 是同一网站的两条管线 ,
///     共用 FromTypeOfNews.WscnMedia , 分别注册在 NewsSpiderRegistry 与 FlashNewsSpiderRegistry。
///     列表与详情都是 api-one-wscn.awtmt.com 的免签 JSON 接口 ( 2026-09 实测 ) :
///     列表游标单调向旧、页间零重叠 ; 详情正文为 HTML 片段 ( 标签集 p/h2/img/blockquote/strong/span/div )。
/// </summary>
public static class WscnArticleResource
{
    /// <summary>
    ///     文章列表接口 : ?limit={单页条数}[&amp;cursor={上一页 next_cursor}] ; 游标为响应里的 next_cursor
    ///     ( "最新时间,最老时间" 对 , 接口自解释 , 调用方透传 )
    /// </summary>
    public const string ArticlesListUrl = "https://api-one-wscn.awtmt.com/apiv1/content/articles";

    /// <summary>
    ///     文章详情接口 , {0} 为文章 id ( 从条目 uri 末段提取 )。
    ///     实测坑 : extract 参数必填 ( 0=带图 HTML / 1=纯文本提取无图 ) , 缺失报 code 60327 "extract 不正确"
    /// </summary>
    public const string ArticleDetailUrlTemplate = "https://api-one-wscn.awtmt.com/apiv1/content/articles/{0}?extract=0";

    public const string ResourceHost = "api-one-wscn.awtmt.com";

    public const string NewsFromName = "华尔街见闻";

    /// <summary>
    ///     单页上限 : 实测 limit 超过 30 时接口返回 data 为空字符串 ( code 仍 20000 OK ,
    ///     不是钳制而是静默无数据 ) , 请求侧必须钳制到 30
    /// </summary>
    public const int MaxPageSize = 30;

    /// <summary>
    ///     条目 uri 形如 https://wallstreetcn.com/articles/3782716 , 文章 id 取末段数字
    /// </summary>
    public const string ArticleIdPattern = @"/articles/(\d+)";

    /// <summary>
    ///     文章源栏目 : 只配一个"文章全量流"栏目 , 不按 categories 建多栏目 ——
    ///     实测 global 标签覆盖 119/120 ( 全量流本身就是完整列表 ) , 按类别建多栏目会把同一批文章重复抓 N 遍 ,
    ///     分类号改为逐条从条目的 categories 按优先级推断 ( 见 <see cref="InferCategory" /> )
    /// </summary>
    public const string ColumnId = "articles";

    public const string ColumnName = "文章要闻";

    /// <summary>全量流的兜底分类号 ( 要闻 / 未匹配到已知类别 )</summary>
    public const int DefaultCategoryNumber = 302;

    /// <summary>
    ///     类别标签 → 分类号 , 数组顺序即匹配优先级 ( 先垂直后宽泛 ) :
    ///     条目的 categories 是无序多标签 ( 如 ["global","us-shares","ai"] ) ,
    ///     按本表顺序取第一个命中的标签落库 , 都不命中归 302 要闻。
    ///     分类号段 302-312 接在见闻快讯 301 之后 , 仍在见闻 301-399 段内
    /// </summary>
    public static readonly (string[] Slugs, int CategoryNumber)[] CategoryRuleList =
    {
        (["ai", "technology", "electronic"], 309),                       // AI 科技
        (["new-energy-vehicle", "energy-storage", "medicine", "tmt-firm", "enterprise"], 310), // 产业公司
        (["wscn-ipo"], 311),                                             // IPO
        (["etf"], 308),                                                  // 基金 ETF
        (["forex"], 307),                                                // 外汇
        (["bonds"], 305),                                                // 债券
        (["commodities"], 306),                                          // 商品
        (["us-shares"], 304),                                            // 美股
        (["shares"], 303)                                                // A股
    };

    /// <summary>按优先级把条目的类别标签列表映射为分类号 , 不命中归 302 要闻</summary>
    public static int InferCategory(string[]? categories)
    {
        if (categories == null || categories.Length == 0) return DefaultCategoryNumber;
        foreach (var (slugs, categoryNumber) in CategoryRuleList)
            if (categories.Any(slug => slugs.Contains(slug)))
                return categoryNumber;
        return DefaultCategoryNumber;
    }
}
