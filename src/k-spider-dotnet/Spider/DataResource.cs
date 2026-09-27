namespace KSpider.Spider;

/// <summary>
///     新闻栏目分类 ( 名称 + 分类号 ) , 分类号随列表行落 spider_news_list.category ;
///     编号 1-22 , 多个栏目可共用同一分类 ( 见 DfNewsResource )
/// </summary>
public struct NewsCategory
{
    public static readonly NewsCategory CategoryIntroduction = new()
    {
        CategoryName = "导读",
        CategoryNumber = 1
    };

    public static readonly NewsCategory CategoryCommentary = new()
    {
        CategoryName = "时评",
        CategoryNumber = 2
    };

    public static readonly NewsCategory CategoryStockReview = new()
    {
        CategoryName = "股评",
        CategoryNumber = 3
    };

    public static readonly NewsCategory CategoryDomesticEconomy = new()
    {
        CategoryName = "国内经济",
        CategoryNumber = 4
    };

    public static readonly NewsCategory CategorySecuritiesFocus = new()
    {
        CategoryName = "证劵聚焦",
        CategoryNumber = 5
    };

    public static readonly NewsCategory CategoryInternationalEconomic = new()
    {
        CategoryName = "国际经济",
        CategoryNumber = 6
    };

    public static readonly NewsCategory CategoryMacroResearch = new()
    {
        CategoryName = "宏观研究",
        CategoryNumber = 7
    };

    public static readonly NewsCategory CategoryAShare = new()
    {
        CategoryName = "沪深股",
        CategoryNumber = 8
    };

    public static readonly NewsCategory CategoryHkStock = new()
    {
        CategoryName = "港股",
        CategoryNumber = 9
    };

    public static readonly NewsCategory CategoryChineseConceptStocks = new()
    {
        CategoryName = "中概股市",
        CategoryNumber = 10
    };

    public static readonly NewsCategory CategoryEa = new()
    {
        CategoryName = "欧美股市",
        CategoryNumber = 11
    };

    public static readonly NewsCategory CategoryIeConsulting = new()
    {
        CategoryName = "产经咨询",
        CategoryNumber = 12
    };

    public static readonly NewsCategory CategoryBusinessConsulting = new()
    {
        CategoryName = "商业咨询",
        CategoryNumber = 13
    };

    public static readonly NewsCategory CategoryIndustryResearch = new()
    {
        CategoryName = "行业研究",
        CategoryNumber = 14
    };

    public static readonly NewsCategory CategoryWealthWatch = new()
    {
        CategoryName = "财富观察",
        CategoryNumber = 15
    };

    public static readonly NewsCategory CategoryHotspotScanning = new()
    {
        CategoryName = "热点扫描",
        CategoryNumber = 16
    };

    public static readonly NewsCategory CategoryInDepthInvestigation = new()
    {
        CategoryName = "纵深调查",
        CategoryNumber = 17
    };

    public static readonly NewsCategory CategoryIndustryPerspective = new()
    {
        CategoryName = "产业透视",
        CategoryNumber = 18
    };

    public static readonly NewsCategory CategoryBusinessObservation = new()
    {
        CategoryName = "商业观察",
        CategoryNumber = 19
    };

    public static readonly NewsCategory CategoryEntrepreneurshipStudies = new()
    {
        CategoryName = "创业研究",
        CategoryNumber = 20
    };

    public static readonly NewsCategory CategoryNewStocksSectorsFutures = new()
    {
        CategoryName = "新股版块期货",
        CategoryNumber = 21
    };

    public static readonly NewsCategory CategoryYtStock = new()
    {
        CategoryName = "亚太",
        CategoryNumber = 22
    };

    public string CategoryName { get; set; }
    public int CategoryNumber { get; set; }
}

// 记录从哪个渠道抓取的新闻
// 枚举标识"网站来源" , 一个网站一个值 ; 同一网站有两种内容形态时 ( 如财联社的电报 + 文章频道 )
// 分属两个注册表 ( FlashNewsSpiderRegistry / NewsSpiderRegistry ) , 共用同一个枚举值
public enum FromTypeOfNews
{
    // 东方财富
    DfMedia = 1,

    // 财联社 ( 电报走实时快讯型管线 , 文章频道走网页抓取型管线 )
    ClsMedia = 2,

    // 新浪财经 ( 7x24 快讯 )
    SinaMedia = 3,

    // 华尔街见闻 ( live 快讯 )
    WscnMedia = 4,

    // 金十数据 ( 快讯 )
    Jin10Media = 5
}

/// <summary>
///     列表行状态机 : 0 未下载 → 3 已下载原始 → 1 已解析详情 ; 失败态 2 解析失败 / 4 下载失败 ,
///     fail_count 未达 NewsPipelineConst.MaxFailCount 时自动重试
/// </summary>
public enum NewsDownloadStatusCode
{
    NoDownload = 0,
    SuccessSyncDetailInfo = 1,
    FailedSyncDetailInfo = 2,
    SuccessDownloadOriginInfo = 3,
    FailedDownloadOriginInfo = 4
}

/// <summary>
///     新闻流水线公共定义
/// </summary>
public static class NewsPipelineConst
{
    /// <summary>
    ///     单条新闻在下载/解析阶段的累计失败次数上限 , 达到后不再重试 ( 终态 2 / 4 )
    /// </summary>
    public const int MaxFailCount = 3;
}

/// <summary>
///     原始内容的存储形态
/// </summary>
public enum NewsContentOriginType
{
    Json = 1,
    Xml = 2
}

/// <summary>
///     单条原始内容的下载结果
/// </summary>
public enum NewsContentOriginStatus
{
    Success = 1,
    Failed = 2
}
