using KSpider.Spider.News;

namespace KSpider.Spider.ClsNews;

/// <summary>
///     财联社接口常量与栏目定义 ( 参数与签名算法由前端 bundle 实测得出 )
/// </summary>
public static class ClsNewsResource
{
    // 电报列表接口 : 无需登录与 Cookie , 参数见 GetListPage
    public const string RollListUrl = "https://www.cls.cn/v1/roll/get_roll_list";
    public const string ResourceHost = "www.cls.cn";

    public const string App = "CailianpressWeb";
    public const string Os = "web";

    /// <summary>
    ///     前端版本号 , 参与签名 ; 财联社升级前端后需同步更新 ( 失效表现 : errno 10012 签名错误 )
    /// </summary>
    public const string Sv = "8.7.9";

    /// <summary>
    ///     单页上限 , rn 超过 50 会被静默返回空数组 ( 实测 )
    /// </summary>
    public const int MaxPageSize = 50;

    /// <summary>
    ///     详情页地址作为 news_url 的稳定形态 ; 不用接口返回的 shareurl —— 它带 sv 参数 , 版本一变去重键就变
    /// </summary>
    public const string DetailUrlTemplate = "https://www.cls.cn/detail/{0}";

    /// <summary>
    ///     财联社源内部栏目编号段 ( 东财占用 1-22 )
    /// </summary>
    public const int TelegraphCategoryNumber = 101;

    public const string NewsFromName = "财联社";

    /// <summary>
    ///     v1 只接 "全部电报" 一个栏目
    /// </summary>
    public static readonly NewsColumn TelegraphColumn = new("telegraph", "电报");
}
