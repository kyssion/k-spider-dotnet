using System.Text.Encodings.Web;
using System.Text.Json;
using k_spider_dotnet.script.df_news.spider;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources;

namespace k_spider_dotnet_test.spider_test.df_news;
[TestClass]
public class DfListSpiderTest
{
    [TestMethod]
    public void TestGetDfList()
    {
        var startTime = DateTime.Now;
        var dfListItem = new DfListSpider();
        var list = dfListItem.GetDfListInfo("https://finance.eastmoney.com/a/ccjdd_{0}.html", 10, null, true, false).Result;
        Console.WriteLine(DateTime.Now.Subtract(startTime).TotalMilliseconds);
    }

    [TestMethod]
    public void TestGetDfListByUrl()
    {
        var needUrlInfo = DfResource.DfListUrlResourceList[0];
        var dfListItem = new DfListSpider().GetDfListInfoByUrl(needUrlInfo.ListResourceNumber,1,25,200,DfListOrderType.ByHeat).Result;
        Console.WriteLine(JsonSerializer.Serialize(dfListItem, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        }));
    }
}