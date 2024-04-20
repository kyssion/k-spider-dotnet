using k_spider_dotnet.script.df_news.spider;

namespace k_spider_dotnet.develop.script;

// 东方财富信息抓去初始化信息check
public static class DfSpiderScriptCheck
{
    // 使用playwrigth抓去页面信息check ResouceListNum 信息是否和当前配置的信息匹配
    public static void CheckDfListUrlResourceList()
    {
        var dfListSpider = new DfListSpiderWithPlaywright(false, true);
        try
        {
            Task.WhenAll(DfResource.DfListUrlResourceList.Select(resourceItem => Task.Run(async () =>
            {
                var numberInfo = await dfListSpider.GetListResourceNumberInfo(string.Format(resourceItem.Url, 1));
                if (numberInfo != resourceItem.ListResourceNumber)
                {
                    throw new Exception($"module name : {resourceItem.ModuleName} . module number : " +
                                        $"{resourceItem.ListResourceNumber} . number : {numberInfo}");
                }

            })).ToArray()).ContinueWith(t =>
            {
                if (t.Exception == null) return;
                // 处理所有异常
                foreach (var ex in t.Exception.InnerExceptions)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult(); // todo 这里注意一个知识点 ， ContinueWith如果没有异常的话下一步会有问题 ，
                                         // todo task会是IsCanceled = true
        }
        finally
        {
            dfListSpider.Close().GetAwaiter().GetResult();
        }
    }
}