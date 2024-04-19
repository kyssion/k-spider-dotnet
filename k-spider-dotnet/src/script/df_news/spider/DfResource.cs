namespace k_spider_dotnet.script.df_news.spider;

public static class DfResource
{
    // 列表页面请求数据的url地址
    public const string RequestDfListUrl = "https://np-listapi.eastmoney.com/comm/web/getNewsByColumns?client=web&biz=web_news_col&column={0}&order={1}&page_index={2}&page_size={3}&req_trace={4}&fields=code,showTime,title,mediaName,summary,image,url,uniqueUrl,Np_dst";
    public struct DfListUrlResource
    {
        public string ModuleName { get; set; }
        // 列表页面的地址数据全集地址
        public string Url { get; set; }
        public int ListResourceNumber{ get; init; }
    }

    public static readonly DfListUrlResource[] DfListUrlResourceList = new DfListUrlResource[]
    {
        new DfListUrlResource()
        {
            ModuleName = "导读",
            Url = "https://finance.eastmoney.com/a/ccjdd_{0}.html",
            ListResourceNumber = 344
        },
        new DfListUrlResource()
        {
            ModuleName = "时评",
            Url = "https://finance.eastmoney.com/a/cjjsp_{0}.html",
        },
        new DfListUrlResource()
        {
            ModuleName = "股评",
            Url = "https://finance.eastmoney.com/a/cgspl_{0}.html",
        },
        new DfListUrlResource()
        {
            ModuleName = "国内经济",
            Url = "https://finance.eastmoney.com/a/cgnjj_{0}.html",
        },
        new DfListUrlResource()
        {
            ModuleName = "证劵聚焦",
            Url = "https://finance.eastmoney.com/a/czqyw_{0}.html",
        },
        new DfListUrlResource()
        {
            ModuleName = "国际经济",
            Url = "https://finance.eastmoney.com/a/cgjjj_{0}.html",
        },
        new DfListUrlResource()
        {
            ModuleName = "宏观研究",
            Url = "https://finance.eastmoney.com/a/chgyj_{0}.html",
        },
        new DfListUrlResource()
        {
            ModuleName = "沪深公司",
            Url = "https://finance.eastmoney.com/a/cssgs_{0}.html",
        },
        new DfListUrlResource()
        {
            ModuleName = "港股公司",
            Url = "https://hk.eastmoney.com/a/cgsbd_{0}.html",
        },
        new DfListUrlResource()
        {
            ModuleName = "中概股",
            Url = "https://stock.eastmoney.com/a/czggng_{0}.html",
        },
        new DfListUrlResource()
        {
            ModuleName = "欧美公司",
            Url = "https://stock.eastmoney.com/a/cmgpj_{0}.html",
        },
        new DfListUrlResource()
        {
            ModuleName = "产经咨询",
            Url = "https://finance.eastmoney.com/a/ccjxw_{0}.html",
        },
        new DfListUrlResource()
        {
            ModuleName = "商业咨询",
            Url = "https://biz.eastmoney.com/a/csyzx_{0}.html",
        },
        new DfListUrlResource()
        {
            ModuleName = "行业研究",
            Url = "https://stock.eastmoney.com/a/chyyj_{0}.html",
        },
        new DfListUrlResource()
        {
            ModuleName = "财富观察",
            Url = "https://enterprise.eastmoney.com/a/ccfgc_{0}.html",
        },
        new DfListUrlResource()
        {
            ModuleName = "热点扫描",
            Url = "https://finance.eastmoney.com/a/crdsm_{0}.html",
        },
        new DfListUrlResource()
        {
            ModuleName = "纵深调查",
            Url = "https://finance.eastmoney.com/a/czsdc_{0}.html",
        },
        new DfListUrlResource()
        {
            ModuleName = "产业透视",
            Url = "https://finance.eastmoney.com/a/ccyts_{0}.html",
        },
        new DfListUrlResource()
        {
            ModuleName = "商业观察",
            Url = "https://finance.eastmoney.com/a/csygc_{0}.html",
        },
        new DfListUrlResource()
        {
            ModuleName = "创业研究",
            Url = "https://enterprise.eastmoney.com/a/ccyyj_{0}.html",
        }
    };
}

public enum DfListOrderType
{
    ByHeat = 1,
    ByTime = 2
}
