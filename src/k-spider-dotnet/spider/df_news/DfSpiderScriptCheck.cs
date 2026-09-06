using k_spider_dotnet.logger;
using k_spider_dotnet.spider.df_news.playwright;
using Microsoft.Extensions.Logging;

namespace k_spider_dotnet.spider.df_news;

// 东方财富信息抓去初始化信息check
public class DfSpiderScriptCheck
{
    private static readonly ILogger Log = LogFactory.GetLogger<DfSpiderScriptCheck>();

    // 使用playwrigth抓去页面信息check ResouceListNum 信息是否和当前配置的信息匹配
    public static void CheckDfListUrlResourceList()
    {
        var dfListSpider = new DfListSpiderWithPlaywright(false, true);
        try
        {
            foreach (var resourceItem in DfNewsResource.DfListUrlResourceList)
            {
                var numberInfo = dfListSpider.GetListResourceNumberInfo(string.Format(resourceItem.Url, 1)).Result;
                if (numberInfo != resourceItem.ListResourceNumber)
                    throw new Exception($"module name : {resourceItem.CategoryInfo.CategoryNumber} . module number : " +
                                        $"{resourceItem.ListResourceNumber} . number : {numberInfo}");
            }
        }
        finally
        {
            dfListSpider.Close().GetAwaiter().GetResult();
        }
    }
}