namespace k_spider_dotnet.script.df_news.spider;

public static class DfResource
{
    // 列表页面请求数据的url地址
    public const string RequestDfListUrl = "https://np-listapi.eastmoney.com/comm/web/getNewsByColumns?client=web&biz=web_news_col&column={0}&order={1}&page_index={2}&page_size={3}&req_trace={4}&fields=code,showTime,title,mediaName,summary,image,url,uniqueUrl,Np_dst";

    public const string ListResourceHost = "np-listapi.eastmoney.com";
    public const string ContextResourceHost = "finance.eastmoney.com";
    public struct DfListUrlResource
    {
        public NewsCategory CategoryInfo { get; set; }
        // 列表页面的地址数据全集地址
        public string Url { get; set; }
        public int ListResourceNumber{ get; set; }
    }

    public static readonly DfListUrlResource[] DfListUrlResourceList = new DfListUrlResource[]
    {
        new DfListUrlResource()
        {
            CategoryInfo = NewsCategory.CategoryIntroduction,
            Url = "https://finance.eastmoney.com/a/ccjdd_{0}.html",
            ListResourceNumber = 344
        },
        new DfListUrlResource()
        {
            CategoryInfo = NewsCategory.CategoryCommentary,
            Url = "https://finance.eastmoney.com/a/cjjsp_{0}.html",
            ListResourceNumber = 371
        },
        new DfListUrlResource()
        {
            CategoryInfo = NewsCategory.CategoryStockReview,
            Url = "https://finance.eastmoney.com/a/cgspl_{0}.html",
            ListResourceNumber = 374,
        },
        new DfListUrlResource()
        {
            CategoryInfo = NewsCategory.CategoryDomesticEconomy,
            Url = "https://finance.eastmoney.com/a/cgnjj_{0}.html",
            ListResourceNumber = 350,
        },
        new DfListUrlResource()
        {
            CategoryInfo = NewsCategory.CategorySecuritiesFocus,
            Url = "https://finance.eastmoney.com/a/czqyw_{0}.html",
            ListResourceNumber = 353,
        },
        new DfListUrlResource()
        {
            CategoryInfo = NewsCategory.CategoryInternationalEconomic,
            Url = "https://finance.eastmoney.com/a/cgjjj_{0}.html",
            ListResourceNumber =351,
        },
        new DfListUrlResource()
        {
            CategoryInfo = NewsCategory.CategoryMacroResearch,
            Url = "https://finance.eastmoney.com/a/chgyj_{0}.html",
            ListResourceNumber = 352,
        },
        new DfListUrlResource()
        {
            CategoryInfo = NewsCategory.CategoryAShareCompanies,
            Url = "https://finance.eastmoney.com/a/cssgs_{0}.html",
            ListResourceNumber = 349,
        },
        new DfListUrlResource()
        {
            CategoryInfo = NewsCategory.CategoryHkStockCompanies,
            Url = "https://hk.eastmoney.com/a/cgsbd_{0}.html",
            ListResourceNumber = 535,
        },
        new DfListUrlResource()
        {
            CategoryInfo = NewsCategory.CategoryChineseConceptStocks,
            Url = "https://stock.eastmoney.com/a/czggng_{0}.html",
            ListResourceNumber = 437,
        },
        new DfListUrlResource()
        {
            CategoryInfo = NewsCategory.CategoryEaCompanies,
            Url = "https://stock.eastmoney.com/a/cmgpj_{0}.html",
            ListResourceNumber = 440,
        },
        new DfListUrlResource()
        {
            CategoryInfo = NewsCategory.CategoryIeConsulting,
            Url = "https://finance.eastmoney.com/a/ccjxw_{0}.html",
            ListResourceNumber = 355,
        },
        new DfListUrlResource()
        {
            CategoryInfo = NewsCategory.CategoryBusinessConsulting,
            Url = "https://biz.eastmoney.com/a/csyzx_{0}.html",
            ListResourceNumber = 670,
        },
        new DfListUrlResource()
        {
            CategoryInfo = NewsCategory.CategoryIndustryResearch,
            Url = "https://stock.eastmoney.com/a/chyyj_{0}.html",
            ListResourceNumber = 421,
        },
        new DfListUrlResource()
        {
            CategoryInfo = NewsCategory.CategoryWealthWatch,
            Url = "https://enterprise.eastmoney.com/a/ccfgc_{0}.html",
            ListResourceNumber = 1138,
        },
        new DfListUrlResource()
        {
            CategoryInfo = NewsCategory.CategoryHotspotScanning,
            Url = "https://finance.eastmoney.com/a/crdsm_{0}.html",
            ListResourceNumber = 365,
        },
        new DfListUrlResource()
        {
            CategoryInfo = NewsCategory.CategoryInDepthInvestigation,
            Url = "https://finance.eastmoney.com/a/czsdc_{0}.html",
            ListResourceNumber = 363,
        },
        new DfListUrlResource()
        {
            CategoryInfo = NewsCategory.CategoryIndustryPerspective,
            Url = "https://finance.eastmoney.com/a/ccyts_{0}.html",
            ListResourceNumber = 372,
        },
        new DfListUrlResource()
        {
            CategoryInfo = NewsCategory.CategoryBusinessObservation,
            Url = "https://finance.eastmoney.com/a/csygc_{0}.html",
            ListResourceNumber = 373,
        },
        new DfListUrlResource()
        {
            CategoryInfo = NewsCategory.CategoryEntrepreneurshipStudies,
            Url = "https://enterprise.eastmoney.com/a/ccyyj_{0}.html",
            ListResourceNumber = 683,
        }
    };
}

public enum DfListOrderType
{
    ByHeat = 1,
    ByTime = 2
}
