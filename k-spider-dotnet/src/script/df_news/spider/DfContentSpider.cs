using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using k_spider_dotnet.script.df_news.spider.model;
using k_spider_dotnet.tool.http;
using k_spider_dotnet.tool.log;
using k_spider_dotnet.tool.resource;
using Microsoft.Extensions.Logging;

namespace k_spider_dotnet.script.df_news.spider;

public partial class DfContentSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<DfListSpider>();

    [GeneratedRegex(@"\s")]
    private static partial Regex FillWriteLine();

    public async Task<DfContentInfo> GetDfContextInfoByUrl(string url)
    {
        try
        {
            var responseString = await new HttpClient().GetStringAsync(url);
            return GetDfContextInfoByHtml(url, responseString);
        }
        catch (Exception e)
        {
            Log.LogError("[GetDfContextInfoByUrl] 拉取详情失败 url : {} , err : {}", url, e);
            throw;
        }
    }

    public async Task<DfContentInfo> GetDfContextInfoByUrlInterface(string url)
    {
        var lastPath = HttpUrlTool.GetUrlLastPath(url);
        var paramsNumber = lastPath.Split(".", 2)[0].Split(",", 2)[^1];
        var newUrl = string.Format(DfResource.RequestDfContextUrl, paramsNumber, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        try
        {
            DfContentInfo ans = new();
            var responseString = await new HttpClient().GetStringAsync(newUrl);
            var forecastNode = JsonNode.Parse(responseString)!;
            var jsonData = forecastNode["data"];
            if (jsonData == null) throw new Exception("[GetDfContextInfoByInterface] 获取信息失败");

            ans.NewsTitle = jsonData["Art_Title"]?.ToString() ?? "";
            ans.NewsSummary = jsonData["Art_Title"]?.ToString() ?? "";
            ans.NewsFrom = jsonData["Art_Media_Name"]?.ToString() ?? "";
            ans.NewsTime = jsonData["Art_ShowTime"]?.ToString() ?? "";
            ans.NewsKeyword = jsonData["Art_Keyword"]?.ToString() ?? "";
            ans.NewsUrl = url;
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(jsonData["Art_Content"]?.ToString() ?? "");
            return FillContentInfoByHtmlNode(ans, htmlDoc.DocumentNode);
        }
        catch (Exception e)
        {
            Log.LogError("[GetDfContextInfoByInterface] 拉取详情失败 url : {} , newUrl :P {}, err : {}", url, newUrl, e);
            throw;
        }
    }

    private DfContentInfo FillContentInfoByHtmlNode(DfContentInfo ans, HtmlNode node)
    {
        var txtInfNodes = node.ChildNodes;
        var contextDetails = new List<DfContextDetailInfo>();
        var ti = CultureInfo.CurrentCulture.TextInfo;
        foreach (var infosNode in txtInfNodes)
            if (infosNode.NodeType != HtmlNodeType.Text && infosNode.NodeType != HtmlNodeType.Comment)
                switch ((HtmlTagName)Enum.Parse(typeof(HtmlTagName), ti.ToTitleCase(infosNode.Name)))
                {
                    case HtmlTagName.Center:
                        var imgItem = infosNode.SelectSingleNode("./img");
                        if (imgItem == null) continue;
                        var imgUrl = imgItem.Attributes["src"].Value;
                        contextDetails.Add(new DfContextDetailInfo
                        {
                            TagType = HtmlTagName.Center.ToString(),
                            Value = "",
                            ValueType = "IMG",
                            ResourceUri = imgUrl
                        });
                        break;
                    case HtmlTagName.P:
                        if (infosNode.Attributes["class"] != null) continue;

                        var pImgItem = infosNode.SelectSingleNode("./img");
                        if (pImgItem != null)
                        {
                            var pImgItemUrl = pImgItem.Attributes["src"].Value;
                            contextDetails.Add(new DfContextDetailInfo
                            {
                                TagType = HtmlTagName.P.ToString(),
                                Value = "",
                                ValueType = "IMG",
                                ResourceUri = pImgItemUrl
                            });
                        }
                        else
                        {
                            var detailValues = FillWriteLine().Replace(infosNode.InnerText, "");
                            if (detailValues.StartsWith("主力资金加仓名单实时更新")) continue;
                            contextDetails.Add(new DfContextDetailInfo
                            {
                                TagType = HtmlTagName.P.ToString(),
                                Value = detailValues,
                                ValueType = "TEXT"
                            });
                        }

                        break;
                    case HtmlTagName.H1:
                    case HtmlTagName.H2:
                    case HtmlTagName.H3:   
                    case HtmlTagName.H4:   
                    case HtmlTagName.H5:  
                        contextDetails.Add(new DfContextDetailInfo
                        {
                            TagType = HtmlTagName.H.ToString(),
                            Value =  FillWriteLine().Replace(infosNode.InnerText, ""),
                            ValueType = "TEXT"
                        });
                        break;
                    case HtmlTagName.Div:
                    default:
                        continue;
                }

        ans.NewsDataContent = contextDetails;
        (ans.NewsDataContentText, ans.ImgInfos) = GetAllContextDetailAndImgInfoList(contextDetails);
        return ans;
    }

    private DfContentInfo GetDfContextInfoByHtml(string url, string responseString)
    {
        var ans = new DfContentInfo
        {
            NewsFrom = url
        };
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(responseString);
        // 1. 获取 标题 , 信息来源 , 新闻时间
        var htmlNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='contentwrap']");
        if (htmlNode != null) return GetDfContextInfoByNewHtml(url, htmlNode);
        htmlNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='newsContent']");
        if (htmlNode != null) return GetDfContextInfoByOldHtml(url, htmlNode);

        throw new Exception($"当前html 格式暂时没有支持 ， url {url}");
    }

    private DfContentInfo GetDfContextInfoByOldHtml(string url, HtmlNode nowHtmlNode)
    {
        var ans = new DfContentInfo
        {
            NewsFrom = url
        };
        var titleNode = nowHtmlNode.SelectSingleNode("//h1");
        ans.NewsTitle = titleNode.InnerText;
        ans.NewsTime = FillWriteLine().Replace(nowHtmlNode.SelectSingleNode("//div[@class='time']").InnerText, "");
        ans.NewsFrom = FillWriteLine().Replace(nowHtmlNode.SelectSingleNode("//div[@class='source']").InnerText, "");
        ans.NewsSummary = FillWriteLine()
            .Replace(nowHtmlNode.SelectSingleNode("//div[@class='b-review']")?.InnerText ?? "", "");
        return FillContentInfoByHtmlNode(ans, nowHtmlNode.SelectSingleNode("//div[@id='ContentBody']"));
    }

    private DfContentInfo GetDfContextInfoByNewHtml(string url, HtmlNode nowHtmlNode)
    {
        var ans = new DfContentInfo
        {
            NewsFrom = url
        };
        var titleNode = nowHtmlNode.SelectSingleNode("//div[@class='title']");
        ans.NewsTitle = titleNode.InnerText;
        var tipBoxNodeChildNodes = nowHtmlNode.SelectNodes("//div[@class='tipbox']/div[@class='infos']/div");
        ans.NewsTime = FillWriteLine().Replace(tipBoxNodeChildNodes[0].InnerText, "");
        ans.NewsFrom = FillWriteLine().Replace(tipBoxNodeChildNodes[1].InnerText, "");
        ans.NewsSummary = FillWriteLine()
            .Replace(nowHtmlNode.SelectSingleNode("//div[@class='abstract']/div[@class='txt']")?.InnerText ?? "", "");
        var txtInfosNodes = nowHtmlNode.SelectSingleNode("//div[@class='txtinfos']").ChildNodes;
        return FillContentInfoByHtmlNode(ans, nowHtmlNode.SelectSingleNode("//div[@id='txtinfos']"));
    }

    // 下载图片信息
    private (string allDetailInfos, List<HtmlGetImgDownLoad.ImgInfo> allImgInfo) GetAllContextDetailAndImgInfoList(
        IReadOnlyList<DfContextDetailInfo> contextDetails)
    {
        var allDetailInfos = "";
        var imgUrlList = new Dictionary<int, string>();
        for (var a = 0; a < contextDetails.Count; a++)
        {
            var detailInfo = contextDetails[a];
            if (detailInfo is { ValueType: "IMG", ResourceUri: not null }) imgUrlList.Add(a, detailInfo.ResourceUri);
            if (detailInfo.ValueType == "TEXT") allDetailInfos += detailInfo.Value + "\n";
        }

        var taskList = imgUrlList
            .Select(keyValue => HtmlGetImgDownLoad.DownloadImgAsByte(keyValue.Value)).ToList();
        foreach (var task in taskList) task.Wait();
        var allImgInfo = taskList.Select(itemTask => itemTask.Result).ToList();
        return (allDetailInfos, allImgInfo);
    }
}