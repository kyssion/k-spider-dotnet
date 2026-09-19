using KSpider.Spider.News.Web;

namespace KSpider.Spider.News.Flash.Sina;

/// <summary>
///     新浪财经 7x24 接口常量与栏目定义 ( 参数与响应结构为实测结果 )
/// </summary>
public static class SinaNewsResource
{
    // 7x24 直播接口 : 无需鉴权 , 按 page 翻页
    public const string FeedUrl = "https://zhibo.sina.com.cn/api/zhibo/feed";
    public const string ResourceHost = "zhibo.sina.com.cn";

    /// <summary>
    ///     财经直播频道号 ( 7x24 ) ; 接口其余固定参数见 GetListPage
    /// </summary>
    public const int ZhiboId = 152;

    /// <summary>
    ///     单页上限 , 实测 page_size=100 可用 ; 取 50 与其它快讯源保持一致
    /// </summary>
    public const int MaxPageSize = 50;

    /// <summary>
    ///     docurl 缺失时的合成去重键 ( 实测 100 条里约 1 条缺 docurl , 指向 7x24 列表页 )
    /// </summary>
    public const string FallbackNewsUrlTemplate = "https://finance.sina.com.cn/7x24/#feed-{0}";

    /// <summary>
    ///     新浪源内部栏目编号段
    /// </summary>
    public const int LiveCategoryNumber = 201;

    public const string NewsFromName = "新浪财经";

    /// <summary>
    ///     v1 只接 "7x24 直播" 一个栏目
    /// </summary>
    public static readonly NewsColumn LiveColumn = new("7x24", "7x24");
}
