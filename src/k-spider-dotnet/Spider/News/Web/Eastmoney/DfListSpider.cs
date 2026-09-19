using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using KSpider.Common.Logger;
using KSpider.Exceptions;
using KSpider.Spider.News.Web.Eastmoney.Model;
using KSpider.Tool.Http;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.News.Web.Eastmoney;

public class DfListSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<DfListSpider>();


    public async Task<List<DfListInfo>> GetDfListInfoByUrl(DfNewsResource.DfListUrlResource dfListResourceInfo,
        int pageStartNumber,
        int pageEndNumber, int pageSize, DfListOrderType orderType)
    {
        var ans = new List<DfListInfo>();
        while (pageStartNumber <= pageEndNumber)
        {
            ans.AddRange(await GetDfListInfoByPageUrl(dfListResourceInfo, pageStartNumber, pageSize, orderType));
            pageStartNumber++;
        }

        return ans;
    }


    /// <summary>
    ///     抓取指定栏目的单页列表
    /// </summary>
    public async Task<List<DfListInfo>> GetDfListInfoByPageUrl(DfNewsResource.DfListUrlResource dfListResourceInfo,
        int pageNumber, int pageSize,
        DfListOrderType orderType)
    {
        var urlNow = string.Format(DfNewsResource.RequestDfListUrl, dfListResourceInfo.ListResourceNumber, (int)orderType,
            pageNumber, pageSize,
            DateTime.Now.Millisecond);
        try
        {
            var responseString = await HttpClientTools.CreateByHost(DfNewsResource.ListResourceHost).GetStringAsync(urlNow);
            return ParseListResponse(responseString, dfListResourceInfo.CategoryInfo.CategoryNumber);
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

    /// <summary>
    ///     解析列表接口响应 ( 独立成公开静态方法供离线测试 )
    /// </summary>
    public static List<DfListInfo> ParseListResponse(string responseString, int categoryNumber)
    {
        var forecastNode = JsonNode.Parse(responseString)!;
        var jsonData = forecastNode["data"];
        if (jsonData?["list"] == null) throw new Exception("not find date");

        var jsonDataList = (JsonArray)jsonData["list"]!;

        return jsonDataList.OfType<JsonNode>()
            .Select(dataItem => new DfListInfo
            {
                NewsUrl = dataItem["url"]?.ToString() ?? "",
                NewsTitle = dataItem["title"]?.ToString() ?? "",
                NewsSummary = dataItem["summary"]?.ToString() ?? "",
                NewsTime = dataItem["showTime"]?.ToString() ?? "",
                FromMedia = FromTypeOfNews.DfMedia,
                NewsFrom = dataItem["mediaName"]?.ToString() ?? "",
                NewsDownloadTime = DateTime.Now,
                Category = categoryNumber
            })
            .ToList();
    }
}