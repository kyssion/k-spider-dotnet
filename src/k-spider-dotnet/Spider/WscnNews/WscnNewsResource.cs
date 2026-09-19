using KSpider.Spider.News;

namespace KSpider.Spider.WscnNews;

/// <summary>
///     华尔街见闻 live 接口常量与栏目定义 ( 参数与响应结构为实测结果 )
/// </summary>
public static class WscnNewsResource
{
    // 实时快讯接口 : 无需鉴权 , 按 cursor 翻页 ( 响应的 data.next_cursor 直接作为下一页游标 )
    public const string LivesUrl = "https://api-one.wallstcn.com/apiv1/content/lives";
    public const string ResourceHost = "api-one.wallstcn.com";

    /// <summary>
    ///     全球宏观频道 ; 另有 A 股 / 外汇 / 商品等频道 , v1 只接全球宏观
    /// </summary>
    public const string GlobalChannel = "global-channel";

    public const string Client = "pc";

    /// <summary>
    ///     单页上限 , 实测 limit=100 可用 ; 取 50 与其它快讯源保持一致
    /// </summary>
    public const int MaxPageSize = 50;

    /// <summary>
    ///     uri 缺失时的合成去重键 ( 实测 100 条 uri 全非空 )
    /// </summary>
    public const string FallbackNewsUrlTemplate = "https://wallstreetcn.com/livenews/{0}";

    /// <summary>
    ///     见闻源内部栏目编号段
    /// </summary>
    public const int LiveCategoryNumber = 301;

    public const string NewsFromName = "华尔街见闻";

    /// <summary>
    ///     v1 只接 "全球宏观" 一个栏目
    /// </summary>
    public static readonly NewsColumn GlobalColumn = new("global", "全球宏观");
}
