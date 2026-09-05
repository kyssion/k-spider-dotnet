namespace k_spider_dotnet.script.df_news.spider;

public static class DfNewsResource
{
    // 列表页面请求数据的url地址
    public const string RequestDfListUrl =
        "https://np-listapi.eastmoney.com/comm/web/getNewsByColumns?client=web&biz=web_news_col&column={0}&order={1}&page_index={2}&page_size={3}&req_trace={4}&fields=code,showTime,title,mediaName,summary,image,url,uniqueUrl,Np_dst";

    public const string RequestDfContextUrl = "https://newsinfo.eastmoney.com/kuaixun/v2/api/article/{0}?guid={1}";
    public const string ListResourceHost = "np-listapi.eastmoney.com";
    public const string ContextResourceHost = "finance.eastmoney.com";

    public static readonly DfListUrlResource[] DfListUrlResourceList =
    {
        new() // 财经导读
        {
            CategoryInfo = NewsCategory.CategoryIntroduction,
            Url = "https://finance.eastmoney.com/a/ccjdd_{0}.html",
            ListResourceNumber = 344
        },
        new() // 财经时评论
        {
            CategoryInfo = NewsCategory.CategoryCommentary,
            Url = "https://finance.eastmoney.com/a/cjjsp_{0}.html",
            ListResourceNumber = 371
        },
        new() // 股市评论
        {
            CategoryInfo = NewsCategory.CategoryStockReview,
            Url = "https://finance.eastmoney.com/a/cgspl_{0}.html",
            ListResourceNumber = 374
        },
        new() // 国内经济
        {
            CategoryInfo = NewsCategory.CategoryDomesticEconomy,
            Url = "https://finance.eastmoney.com/a/cgnjj_{0}.html",
            ListResourceNumber = 350
        },
        new() // 证券聚焦
        {
            CategoryInfo = NewsCategory.CategorySecuritiesFocus,
            Url = "https://finance.eastmoney.com/a/czqyw_{0}.html",
            ListResourceNumber = 353
        },
        new() // 国际经济
        {
            CategoryInfo = NewsCategory.CategoryInternationalEconomic,
            Url = "https://finance.eastmoney.com/a/cgjjj_{0}.html",
            ListResourceNumber = 351
        },
        new() // 宏观研究
        {
            CategoryInfo = NewsCategory.CategoryMacroResearch,
            Url = "https://finance.eastmoney.com/a/chgyj_{0}.html",
            ListResourceNumber = 352
        },
        new() // 沪深公司
        {
            CategoryInfo = NewsCategory.CategoryAShare,
            Url = "https://finance.eastmoney.com/a/cssgs_{0}.html",
            ListResourceNumber = 349
        },
        new() // 港股公司
        {
            CategoryInfo = NewsCategory.CategoryHkStock,
            Url = "https://hk.eastmoney.com/a/cgsbd_{0}.html",
            ListResourceNumber = 535
        },
        new() // 中概股公司
        {
            CategoryInfo = NewsCategory.CategoryChineseConceptStocks,
            Url = "https://stock.eastmoney.com/a/czggng_{0}.html",
            ListResourceNumber = 437
        },
        new() //  欧美公司
        {
            CategoryInfo = NewsCategory.CategoryEa,
            Url = "https://stock.eastmoney.com/a/cmgpj_{0}.html",
            ListResourceNumber = 440
        },
        new() // 产经咨询
        {
            CategoryInfo = NewsCategory.CategoryIeConsulting,
            Url = "https://finance.eastmoney.com/a/ccjxw_{0}.html",
            ListResourceNumber = 355
        },
        new() // 商业资讯
        {
            CategoryInfo = NewsCategory.CategoryBusinessConsulting,
            Url = "https://biz.eastmoney.com/a/csyzx_{0}.html",
            ListResourceNumber = 670
        },
        new() // 行业研究
        {
            CategoryInfo = NewsCategory.CategoryIndustryResearch,
            Url = "https://stock.eastmoney.com/a/chyyj_{0}.html",
            ListResourceNumber = 421
        },
        new() // 财富观察
        {
            CategoryInfo = NewsCategory.CategoryWealthWatch,
            Url = "https://enterprise.eastmoney.com/a/ccfgc_{0}.html",
            ListResourceNumber = 1138
        },
        new() // 热点扫描
        {
            CategoryInfo = NewsCategory.CategoryHotspotScanning,
            Url = "https://finance.eastmoney.com/a/crdsm_{0}.html",
            ListResourceNumber = 365
        },
        new() // 纵深调查
        {
            CategoryInfo = NewsCategory.CategoryInDepthInvestigation,
            Url = "https://finance.eastmoney.com/a/czsdc_{0}.html",
            ListResourceNumber = 363
        },
        new() // 产业透视
        {
            CategoryInfo = NewsCategory.CategoryIndustryPerspective,
            Url = "https://finance.eastmoney.com/a/ccyts_{0}.html",
            ListResourceNumber = 372
        },
        new() // 商业观察
        {
            CategoryInfo = NewsCategory.CategoryBusinessObservation,
            Url = "https://finance.eastmoney.com/a/csygc_{0}.html",
            ListResourceNumber = 373
        },
        new() // 创业研究
        {
            CategoryInfo = NewsCategory.CategoryEntrepreneurshipStudies,
            Url = "https://enterprise.eastmoney.com/a/ccyyj_{0}.html",
            ListResourceNumber = 683
        },
        new() //大盘分析
        {
            CategoryInfo = NewsCategory.CategoryHkStock,
            Url = "https://stock.eastmoney.com/a/cdpfx_{0}.html",
            ListResourceNumber = 407
        },
        new() //板块聚焦
        {
            CategoryInfo = NewsCategory.CategoryHkStock,
            Url = "https://stock.eastmoney.com/a/cbkjj_{0}.html",
            ListResourceNumber = 408
        },
        new() //热门股追踪
        {
            CategoryInfo = NewsCategory.CategoryHkStock,
            Url = "https://stock.eastmoney.com/a/cggdj_{0}.html",
            ListResourceNumber = 415
        },
        new() // 主力动态
        {
            CategoryInfo = NewsCategory.CategoryHkStock,
            Url = "https://stock.eastmoney.com/a/czldt_{0}.html",
            ListResourceNumber = 423
        },
        new() // 新股聚焦
        {
            CategoryInfo = NewsCategory.CategoryNewStocksSectorsFutures,
            Url = "https://stock.eastmoney.com/a/cxgyw_{0}.html",
            ListResourceNumber = 448
        },
        new() // 新股策略
        {
            CategoryInfo = NewsCategory.CategoryNewStocksSectorsFutures,
            Url = "https://stock.eastmoney.com/a/cxgcl_{0}.html",
            ListResourceNumber = 449
        },
        new() // 新股评论
        {
            CategoryInfo = NewsCategory.CategoryNewStocksSectorsFutures,
            Url = "https://stock.eastmoney.com/a/cxgpl_{0}.html",
            ListResourceNumber = 450
        },
        new() // 再融资公司聚焦
        {
            CategoryInfo = NewsCategory.CategoryNewStocksSectorsFutures,
            Url = "https://stock.eastmoney.com/a/czrzgsyw_{0}.html",
            ListResourceNumber = 628
        },
        new() // 再融资动态
        {
            CategoryInfo = NewsCategory.CategoryNewStocksSectorsFutures,
            Url = "https://stock.eastmoney.com/a/czrzdt_{0}.html",
            ListResourceNumber = 627
        },
        new() // 期指导读
        {
            CategoryInfo = NewsCategory.CategoryNewStocksSectorsFutures,
            Url = "https://stock.eastmoney.com/a/cqzdd_{0}.html",
            ListResourceNumber = 830
        },
        new() // 港股聚焦
        {
            CategoryInfo = NewsCategory.CategoryHkStock,
            Url = "https://hk.eastmoney.com/a/cggyw_{0}.html",
            ListResourceNumber = 532
        },
        new() // 亚太市场
        {
            CategoryInfo = NewsCategory.CategoryYtStock,
            Url = "https://global.eastmoney.com/a/cytsc_{0}.html",
            ListResourceNumber = 782
        },
        new() // 美股聚焦
        {
            CategoryInfo = NewsCategory.CategoryEa,
            Url = "https://stock.eastmoney.com/a/cmgyw_{0}.html",
            ListResourceNumber = 436
        },
        new() // 欧洲市场
        {
            CategoryInfo = NewsCategory.CategoryEa,
            Url = "https://global.eastmoney.com/a/cozsc_{0}.html",
            ListResourceNumber = 781
        },
        new() // 理财咨询
        {
            CategoryInfo = NewsCategory.CategoryWealthWatch,
            Url = "https://money.eastmoney.com/a/clczx_{0}.html",
            ListResourceNumber = 583
        },
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