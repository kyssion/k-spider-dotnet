using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using k_spider_dotnet.exception;
using k_spider_dotnet.script.df_news.spider.model;
using k_spider_dotnet.tool.http;
using k_spider_dotnet.tool.log;
using Microsoft.Extensions.Logging;

namespace k_spider_dotnet.script.df_news.spider;

public class DfListSpider
{
    private static readonly Regex Regex = new(".(png|jpg|css|aspx|ico)");
    private static readonly ILogger Log = LogFactory.GetLogger<DfListSpider>();


    public async Task<List<DfListInfo>> GetDfListInfoByUrl(DfResource.DfListUrlResource dfListResourceInfo,
        int pageStartNumber,
        int pageEndNumber, int pageSize, DfListOrderType orderType)
    {
        var ans = new List<DfListInfo>();
        while (pageStartNumber <= pageEndNumber)
        {
            ans.AddRange(await GetDfListInfoByUrl(dfListResourceInfo, pageStartNumber, pageSize, orderType));
            pageStartNumber++;
        }

        return ans;
    }


    private async Task<List<DfListInfo>> GetDfListInfoByUrl(DfResource.DfListUrlResource dfListResourceInfo,
        int pageNumber, int pageSize,
        DfListOrderType orderType)
    {
        var urlNow = string.Format(DfResource.RequestDfListUrl, dfListResourceInfo.ListResourceNumber, (int)orderType,
            pageNumber, pageSize,
            DateTime.Now.Millisecond);
        try
        {
            var responseString = await HttpClientTools.Create(DfResource.ListResourceHost).GetStringAsync(urlNow);

            var forecastNode = JsonNode.Parse(responseString)!;
            var jsonData = forecastNode["data"];
            if (jsonData?["list"] == null) throw new Exception("not find date");

            var jsonDataList = (JsonArray)jsonData["list"]!;

            var ans = jsonDataList.OfType<JsonNode>()
                .Select(dataItem => new DfListInfo
                {
                    NewsUrl = dataItem["url"]?.ToString() ?? "",
                    NewsTitle = dataItem["title"]?.ToString() ?? "",
                    NewsSummary = dataItem["summary"]?.ToString() ?? "",
                    NewsTime = dataItem["showTime"]?.ToString() ?? "",
                    FromMedia = FromTypeOfNews.DfMedia,
                    NewsFrom = dataItem["mediaName"]?.ToString() ?? "",
                    NewsDownloadTime = DateTime.Now,
                    Category = dfListResourceInfo.CategoryInfo.CategoryNumber
                })
                .ToList();
            return ans;
        }
        catch (Exception e)
        {
            var message = string.Format(
                "[GetDfListInfoByUrl] has err modelName : {0} , modelNumber : {1} ,pageNumber : {2} , pageSize : {3}, url: {4} , exception : {5}",
                dfListResourceInfo.CategoryInfo.CategoryName, dfListResourceInfo.ListResourceNumber, pageNumber,
                pageSize, urlNow, e);
            Log.LogError(message);
            throw new HtmlFormException(urlNow, message, e);
        }
    }
}