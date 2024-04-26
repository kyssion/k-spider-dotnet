namespace k_spider_dotnet.script;

public struct NewsCategory
{
    public static readonly NewsCategory CategoryIntroduction = new NewsCategory()
    {
        CategoryName = "导读",
        CategoryNumber = 1,
    };
    public static readonly NewsCategory CategoryCommentary = new NewsCategory()
    {
        CategoryName = "时评",
        CategoryNumber = 2,
    };
    public static readonly NewsCategory CategoryStockReview= new NewsCategory()
    {
        CategoryName = "股评",
        CategoryNumber = 3,
    };
    public static readonly NewsCategory CategoryDomesticEconomy = new NewsCategory()
    {
        CategoryName = "国内经济",
        CategoryNumber = 4,
    };
    public static readonly NewsCategory CategorySecuritiesFocus = new NewsCategory()
    {
        CategoryName = "证劵聚焦",
        CategoryNumber = 5,
    };
    public static readonly NewsCategory CategoryInternationalEconomic = new NewsCategory()
    {
        CategoryName = "国际经济",
        CategoryNumber = 6,
    };
    
    public static readonly NewsCategory CategoryMacroResearch = new NewsCategory()
    {
        CategoryName = "宏观研究",
        CategoryNumber = 7,
    };
    public static readonly NewsCategory CategoryAShareCompanies = new NewsCategory()
    {
        CategoryName = "沪深公司",
        CategoryNumber = 8,
    };
    public static readonly NewsCategory CategoryHkStockCompanies = new NewsCategory()
    {
        CategoryName = "港股公司",
        CategoryNumber = 9,
    };
    public static readonly NewsCategory CategoryChineseConceptStocks = new NewsCategory()
    {
        CategoryName = "中概股",
        CategoryNumber = 10,
    };
    public static readonly NewsCategory CategoryEaCompanies = new NewsCategory()
    {
        CategoryName = "欧美公司",
        CategoryNumber = 11,
    };
    public static readonly NewsCategory CategoryIeConsulting = new NewsCategory()
    {
        CategoryName = "产经咨询",
        CategoryNumber = 12,
    };
    public static readonly NewsCategory CategoryBusinessConsulting = new NewsCategory()
    {
        CategoryName = "商业咨询",
        CategoryNumber = 13,
    };
    public static readonly NewsCategory CategoryIndustryResearch = new NewsCategory()
    {
        CategoryName = "行业研究",
        CategoryNumber = 14,
    };
    public static readonly NewsCategory CategoryWealthWatch = new NewsCategory()
    {
        CategoryName = "财富观察",
        CategoryNumber = 15,
    };
    public static readonly NewsCategory CategoryHotspotScanning = new NewsCategory()
    {
        CategoryName = "热点扫描",
        CategoryNumber = 16,
    };
    public static readonly NewsCategory CategoryInDepthInvestigation = new NewsCategory()
    {
        CategoryName = "纵深调查",
        CategoryNumber = 17,
    };
    public static readonly NewsCategory CategoryIndustryPerspective = new NewsCategory()
    {
        CategoryName = "产业透视",
        CategoryNumber = 18,
    };
    public static readonly NewsCategory CategoryBusinessObservation = new NewsCategory()
    {
        CategoryName = "商业观察",
        CategoryNumber = 19,
    };
    public static readonly NewsCategory CategoryEntrepreneurshipStudies = new NewsCategory()
    {
        CategoryName = "创业研究",
        CategoryNumber = 20,
    };
    public string CategoryName { get; set; }
    public int CategoryNumber { get; set; }
}

public static class DataResource{
   
}

