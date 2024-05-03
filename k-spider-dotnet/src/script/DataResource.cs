namespace k_spider_dotnet.script;

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

    public static readonly NewsCategory CategoryAShareCompanies = new()
    {
        CategoryName = "沪深公司",
        CategoryNumber = 8
    };

    public static readonly NewsCategory CategoryHkStockCompanies = new()
    {
        CategoryName = "港股公司",
        CategoryNumber = 9
    };

    public static readonly NewsCategory CategoryChineseConceptStocks = new()
    {
        CategoryName = "中概股",
        CategoryNumber = 10
    };

    public static readonly NewsCategory CategoryEaCompanies = new()
    {
        CategoryName = "欧美公司",
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

    public string CategoryName { get; set; }
    public int CategoryNumber { get; set; }
}


// 记录从哪个渠道抓取的新闻
public enum FromTypeOfNews
{
    // 东方财富
    DfMedia = 1
}

public enum NewsDownloadStatusCode
{
    NoDownload = 0,
    SuccessSyncDetailInfo = 1,
    FailedSyncDetailInfo = 2,
    SuccessDownloadOriginInfo = 3,
    FailedDownloadOriginInfo = 4,
}

public enum NewsContentOriginType
{
    Json = 1,
    Xml = 2, 
}

public enum NewsContentOriginStatus
{
    Success= 1,
    Failed = 2,
}