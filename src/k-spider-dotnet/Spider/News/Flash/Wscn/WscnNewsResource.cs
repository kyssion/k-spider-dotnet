using KSpider.Spider.News.Web;

namespace KSpider.Spider.News.Flash.Wscn;

/// <summary>
///     华尔街见闻 live 接口常量与栏目定义 ( 参数与响应结构为实测结果 )
/// </summary>
public static class WscnNewsResource
{
    // 实时快讯接口 : 无需鉴权 , 按 cursor 翻页 ( 响应的 data.next_cursor 直接作为下一页游标 )
    public const string LivesUrl = "https://api-one.wallstcn.com/apiv1/content/lives";
    public const string ResourceHost = "api-one.wallstcn.com";

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
    ///     见闻源内部栏目编号段起点 ( 全球宏观 301 ; 频道扩展 312-317 接在文章 302-311 之后 , 仍在 301-399 段内 )
    /// </summary>
    public const int LiveCategoryNumber = 301;

    public const string NewsFromName = "华尔街见闻";

    /// <summary>
    ///     live 频道清单 ( 2026-09-30 实测 7 频道全部可用 , code 20000 ) :
    ///     频道间有实质增量 —— 美股/商品频道与全球宏观仅约 7% 重叠 , 全球宏观不是其它频道的超集。
    ///     ColumnId 直接用接口的频道 slug。
    /// </summary>
    public static readonly LiveChannelResource[] LiveChannelResourceList =
    {
        new() { ChannelSlug = "global-channel", ChannelName = "全球宏观", CategoryNumber = 301 },
        new() { ChannelSlug = "a-stock-channel", ChannelName = "A股", CategoryNumber = 312 },
        new() { ChannelSlug = "us-stock-channel", ChannelName = "美股", CategoryNumber = 313 },
        new() { ChannelSlug = "hk-stock-channel", ChannelName = "港股", CategoryNumber = 314 },
        new() { ChannelSlug = "forex-channel", ChannelName = "外汇", CategoryNumber = 315 },
        new() { ChannelSlug = "commodity-channel", ChannelName = "商品", CategoryNumber = 316 },
        new() { ChannelSlug = "bond-channel", ChannelName = "债券", CategoryNumber = 317 }
    };

    public struct LiveChannelResource
    {
        /// <summary>接口 channel 参数 , 同时作为 NewsColumn 的 ColumnId</summary>
        public string ChannelSlug { get; set; }

        public string ChannelName { get; set; }

        public int CategoryNumber { get; set; }
    }
}
