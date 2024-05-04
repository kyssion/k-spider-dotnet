namespace k_spider_dotnet.script.df_news.spider;

public static class DfResource
{
    // 列表页面请求数据的url地址
    public const string RequestDfListUrl =
        "https://np-listapi.eastmoney.com/comm/web/getNewsByColumns?client=web&biz=web_news_col&column={0}&order={1}&page_index={2}&page_size={3}&req_trace={4}&fields=code,showTime,title,mediaName,summary,image,url,uniqueUrl,Np_dst";

    public const string RequestDfContextUrl = "https://newsinfo.eastmoney.com/kuaixun/v2/api/article/{0}?guid={1}";
    public const string ListResourceHost = "np-listapi.eastmoney.com";
    public const string ContextResourceHost = "finance.eastmoney.com";

    public static readonly DfListUrlResource[] DfListUrlResourceList =
    {
        new()
        {
            CategoryInfo = NewsCategory.CategoryIntroduction,
            Url = "https://finance.eastmoney.com/a/ccjdd_{0}.html",
            ListResourceNumber = 344
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryCommentary,
            Url = "https://finance.eastmoney.com/a/cjjsp_{0}.html",
            ListResourceNumber = 371
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryStockReview,
            Url = "https://finance.eastmoney.com/a/cgspl_{0}.html",
            ListResourceNumber = 374
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryDomesticEconomy,
            Url = "https://finance.eastmoney.com/a/cgnjj_{0}.html",
            ListResourceNumber = 350
        },
        new()
        {
            CategoryInfo = NewsCategory.CategorySecuritiesFocus,
            Url = "https://finance.eastmoney.com/a/czqyw_{0}.html",
            ListResourceNumber = 353
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryInternationalEconomic,
            Url = "https://finance.eastmoney.com/a/cgjjj_{0}.html",
            ListResourceNumber = 351
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryMacroResearch,
            Url = "https://finance.eastmoney.com/a/chgyj_{0}.html",
            ListResourceNumber = 352
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryAShare,
            Url = "https://finance.eastmoney.com/a/cssgs_{0}.html",
            ListResourceNumber = 349
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryHkStock,
            Url = "https://hk.eastmoney.com/a/cgsbd_{0}.html",
            ListResourceNumber = 535
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryChineseConceptStocks,
            Url = "https://stock.eastmoney.com/a/czggng_{0}.html",
            ListResourceNumber = 437
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryEa,
            Url = "https://stock.eastmoney.com/a/cmgpj_{0}.html",
            ListResourceNumber = 440
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryIeConsulting,
            Url = "https://finance.eastmoney.com/a/ccjxw_{0}.html",
            ListResourceNumber = 355
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryBusinessConsulting,
            Url = "https://biz.eastmoney.com/a/csyzx_{0}.html",
            ListResourceNumber = 670
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryIndustryResearch,
            Url = "https://stock.eastmoney.com/a/chyyj_{0}.html",
            ListResourceNumber = 421
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryWealthWatch,
            Url = "https://enterprise.eastmoney.com/a/ccfgc_{0}.html",
            ListResourceNumber = 1138
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryHotspotScanning,
            Url = "https://finance.eastmoney.com/a/crdsm_{0}.html",
            ListResourceNumber = 365
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryInDepthInvestigation,
            Url = "https://finance.eastmoney.com/a/czsdc_{0}.html",
            ListResourceNumber = 363
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryIndustryPerspective,
            Url = "https://finance.eastmoney.com/a/ccyts_{0}.html",
            ListResourceNumber = 372
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryBusinessObservation,
            Url = "https://finance.eastmoney.com/a/csygc_{0}.html",
            ListResourceNumber = 373
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryEntrepreneurshipStudies,
            Url = "https://enterprise.eastmoney.com/a/ccyyj_{0}.html",
            ListResourceNumber = 683
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryHkStock,
            Url = "https://stock.eastmoney.com/a/cdpfx_{0}.html",
            ListResourceNumber = 407
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryHkStock,
            Url = "https://stock.eastmoney.com/a/cbkjj_{0}.html",
            ListResourceNumber = 408
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryHkStock,
            Url = "https://stock.eastmoney.com/a/cggdj_{0}.html",
            ListResourceNumber = 415
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryHkStock,
            Url = "https://stock.eastmoney.com/a/czldt_{0}.html",
            ListResourceNumber = 423
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryNewStocksSectorsFutures,
            Url = "https://stock.eastmoney.com/a/cxgyw_{0}.html",
            ListResourceNumber = 448
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryNewStocksSectorsFutures,
            Url = "https://stock.eastmoney.com/a/cxgcl_{0}.html",
            ListResourceNumber = 449
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryNewStocksSectorsFutures,
            Url = "https://stock.eastmoney.com/a/cxgpl_{0}.html",
            ListResourceNumber = 450
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryNewStocksSectorsFutures,
            Url = "https://stock.eastmoney.com/a/czrzgsyw_{0}.html",
            ListResourceNumber = 628
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryNewStocksSectorsFutures,
            Url = "https://stock.eastmoney.com/a/czrzdt_{0}.html",
            ListResourceNumber = 627
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryNewStocksSectorsFutures,
            Url = "https://stock.eastmoney.com/a/cqzdd_{0}.html",
            ListResourceNumber = 830
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryHkStock,
            Url = "https://hk.eastmoney.com/a/cggyw_{0}.html",
            ListResourceNumber = 532
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryYtStock,
            Url = "https://global.eastmoney.com/a/cytsc_{0}.html",
            ListResourceNumber = 782
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryEa,
            Url = "https://stock.eastmoney.com/a/cmgyw_{0}.html",
            ListResourceNumber = 436
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryEa,
            Url = "https://global.eastmoney.com/a/cozsc_{0}.html",
            ListResourceNumber = 781
        },
        new()
        {
            CategoryInfo = NewsCategory.CategoryWealthWatch,
            Url = "https://money.eastmoney.com/a/clczx_{0}.html",
            ListResourceNumber = 583
        }
    };

    public struct DfListUrlResource
    {
        public NewsCategory CategoryInfo { get; set; }

        // 列表页面的地址数据全集地址
        public string Url { get; set; }
        public int ListResourceNumber { get; set; }
    }
}

public enum DfListOrderType
{
    ByHeat = 1,
    ByTime = 2
}