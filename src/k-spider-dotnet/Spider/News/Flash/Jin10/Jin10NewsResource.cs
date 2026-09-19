using KSpider.Spider.News.Web;

namespace KSpider.Spider.News.Flash.Jin10;

/// <summary>
///     金十数据快讯接口常量与栏目定义 ( 参数与响应结构为实测结果 )
/// </summary>
public static class Jin10NewsResource
{
    // 快讯接口 : 必须携带 x-app-id / x-version 两个头 , 否则返回 502
    public const string FlashUrl = "https://flash-api.jin10.com/get_flash_list";
    public const string ResourceHost = "flash-api.jin10.com";

    /// <summary>
    ///     客户端标识头 , 与 x-version 一起写死 ( 实测可用的组合 ; 被拒时对照网页端请求更新 )
    /// </summary>
    public const string AppId = "bVBF4FyRTn5NJF5n";

    public const string Version = "1.0.0";

    /// <summary>
    ///     频道参数 : -8200 = 全部快讯 ( 含重要与非重要 , 条目里用 important 字段区分 )
    /// </summary>
    public const string AllChannel = "-8200";

    /// <summary>
    ///     单条快讯的详情页 ; 接口不自带稳定 URL , 用 id 拼规范形态 ( 实测可访问 )
    /// </summary>
    public const string DetailUrlTemplate = "https://flash.jin10.com/detail/{0}";

    /// <summary>
    ///     金十源内部栏目编号段
    /// </summary>
    public const int FlashCategoryNumber = 401;

    public const string NewsFromName = "金十数据";

    /// <summary>
    ///     v1 只接 "全部快讯" 一个栏目
    /// </summary>
    public static readonly NewsColumn FlashColumn = new("flash", "快讯");
}
