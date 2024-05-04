using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using k_spider_dotnet.exception;
using k_spider_dotnet.script.df_news.spider.model;
using k_spider_dotnet.tool.http;
using k_spider_dotnet.tool.Json;
using k_spider_dotnet.tool.log;
using Microsoft.Extensions.Logging;

namespace k_spider_dotnet.script.df_news.spider;

public partial class DfContentSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<DfListSpider>();

    [GeneratedRegex(@"\s")]
    private static partial Regex FillWriteLine();

    public async Task<DfNewsContentOrigin> GetDfContentOriginInfoByInterface(string url)
    {
        var lastPath = HttpUrlTools.GetUrlLastPath(url);
        var paramsNumber = lastPath.Split(".", 2)[0].Split(",", 2)[^1];
        var ans = new DfNewsContentOrigin
        {
            NewsUrl = url,
            OriginType = NewsContentOriginType.Json,
            NewsOriginContent = ""
        };
        var newUrl = string.Format(DfResource.RequestDfContextUrl, paramsNumber,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        try
        {
            var responseString = await new HttpClient().GetStringAsync(newUrl);
            ans.NewsOriginContent = responseString;
            ans.Status = NewsContentOriginStatus.Success;
            return ans;
        }
        catch (Exception e)
        {
            ans.Message = e.ToString();
            ans.Status = NewsContentOriginStatus.Failed;
            Log.LogError("[GetDfContentOriginInfoByInterface] download error  url : {} , newsUrl : {}, err : {}", url,
                newUrl, e);
        }

        return ans;
    }

    public async Task<DfContentInfo> GetDfContextInfoByUrlInterface(string url)
    {
        var lastPath = HttpUrlTools.GetUrlLastPath(url);
        var paramsNumber = lastPath.Split(".", 2)[0].Split(",", 2)[^1];
        var newUrl = string.Format(DfResource.RequestDfContextUrl, paramsNumber,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        try
        {
            return GetContentInfoByJson(await new HttpClient().GetStringAsync(newUrl), url);
        }
        catch (DownloadHttpRequestException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new HtmlFormException(url,
                $"[GetDfContextInfoByUrlInterface] download error  url : {url} , newsUrl : {newUrl}, err : {e}", e);
        }
    }

    public DfContentInfo GetContentInfoByJson(string responseString, string url)
    {
        try
        {
            DfContentInfo ans = new();
            var forecastNode = JsonNode.Parse(responseString)!;
            var jsonData = forecastNode["data"];
            if (jsonData == null)
                throw new DownloadHttpRequestException(url, "[GetDfContextInfoByInterface] json data not find");
            ans.NewsTitle = jsonData["Art_Title"]?.ToString() ?? "";
            ans.NewsSummary = jsonData["Art_Title"]?.ToString() ?? "";
            ans.NewsFrom = jsonData["Art_Media_Name"]?.ToString() ?? "";
            ans.NewsTime = jsonData["Art_ShowTime"]?.ToString() ?? "";
            ans.NewsKeyword = jsonData["Art_Keyword"]?.ToString() ?? "";
            ans.NewsUrl = url;
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(jsonData["Art_Content"]?.ToString() ?? "");
            return GetDetailInfoByHtml(htmlDoc.DocumentNode, ans);
        }
        catch (DownloadHttpRequestException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new HtmlFormException(url, "[FillContentInfoByHtmlNode] download html form err", e);
        }
    }

    private DfContentInfo GetDetailInfoByHtml(HtmlNode htmlDoc, DfContentInfo ans)
    {
        var txtInfNodes = htmlDoc.ChildNodes;
        var contextDetails = new List<DfContextDetailInfo>();
        var ti = CultureInfo.CurrentCulture.TextInfo;
        string detailValues;
        foreach (var infosNode in txtInfNodes)
        {
            // todo 非法标签。。 后续可以增加非法字符check
            if (ti.ToTitleCase(infosNode.Name) == "Strong<") continue;
            if (infosNode.NodeType == HtmlNodeType.Text)
            {
                detailValues = FillWriteLine().Replace(infosNode.InnerText, "");
                if (detailValues == "") continue;
                contextDetails.Add(new DfContextDetailInfo
                {
                    TagType = HtmlTagName.Table.ToString(),
                    Value = infosNode.InnerText,
                    ValueType = "TEXT"
                });
                continue;
            }

            if (infosNode.NodeType != HtmlNodeType.Comment)
                switch ((HtmlTagName)Enum.Parse(typeof(HtmlTagName), ti.ToTitleCase(infosNode.Name)))
                {
                    case HtmlTagName.Strong:
                        var strongImage = infosNode.SelectSingleNode(".//img");
                        if (strongImage == null)
                        {
                            detailValues = FillWriteLine().Replace(infosNode.InnerText, "");
                            if (detailValues == "") continue;
                            contextDetails.Add(new DfContextDetailInfo
                            {
                                TagType = HtmlTagName.Table.ToString(),
                                Value = infosNode.InnerText,
                                ValueType = "OTHER"
                            });
                            continue;
                        }

                        var strongUrl = strongImage.Attributes["src"].Value;
                        contextDetails.Add(new DfContextDetailInfo
                        {
                            TagType = HtmlTagName.Center.ToString(),
                            Value = "",
                            ValueType = "IMG",
                            ResourceUri = strongUrl
                        });
                        break;
                    case HtmlTagName.Ul:
                        var liNodes = infosNode.SelectNodes("li") ?? infosNode.SelectNodes("ul/li");
                        if (liNodes == null)
                        {
                            detailValues = FillWriteLine().Replace(infosNode.InnerText, "");
                            if (detailValues == "") continue;
                            contextDetails.Add(new DfContextDetailInfo
                            {
                                TagType = HtmlTagName.Table.ToString(),
                                Value = infosNode.InnerText,
                                ValueType = "OTHER"
                            });
                            continue;
                        }

                        if (liNodes.Count == 0) continue;
                        var liList = liNodes.Select(
                            liNode => liNode.InnerText
                        ).ToList();
                        contextDetails.Add(new DfContextDetailInfo
                        {
                            TagType = HtmlTagName.Table.ToString(),
                            Value = JsonUtil.GetJson(liList),
                            ValueType = "UL"
                        });
                        break;
                    case HtmlTagName.Table:
                        var tableValue = new List<List<string>>();
                        var tableChildNodes = infosNode.SelectNodes("tbody/tr") ?? infosNode.SelectNodes("tr");
                        var tableDataList = (from trNode in tableChildNodes
                            select trNode.SelectNodes("td") ?? trNode.SelectNodes("th")
                            into childNodes
                            where childNodes.Count != 0
                            select childNodes.Select(tdNode => tdNode.InnerText).ToList()).ToList();
                        contextDetails.Add(new DfContextDetailInfo
                        {
                            TagType = HtmlTagName.Table.ToString(),
                            Value = JsonUtil.GetJson(tableDataList),
                            ValueType = "TABLE"
                        });
                        break;
                    case HtmlTagName.Center:
                        var imgItem = infosNode.SelectSingleNode(".//img");
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
                        var pImgItem = infosNode.SelectSingleNode(".//img");
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
                            detailValues = FillWriteLine().Replace(infosNode.InnerText, "");
                            if (detailValues == "" || detailValues.StartsWith("主力资金加仓名单实时更新")) continue;
                            contextDetails.Add(new DfContextDetailInfo
                            {
                                TagType = HtmlTagName.P.ToString(),
                                Value = detailValues,
                                ValueType = "TEXT"
                            });
                        }

                        break;
                    case HtmlTagName.H:
                    case HtmlTagName.H1:
                    case HtmlTagName.H2:
                    case HtmlTagName.H3:
                    case HtmlTagName.H4:
                    case HtmlTagName.H5:
                    case HtmlTagName.H6:
                        detailValues = FillWriteLine().Replace(infosNode.InnerText, "");
                        if (detailValues == "") continue;
                        contextDetails.Add(new DfContextDetailInfo
                        {
                            TagType = HtmlTagName.H.ToString(),
                            Value = detailValues,
                            ValueType = "TEXT"
                        });
                        break;
                    case HtmlTagName.Span:
                        detailValues = FillWriteLine().Replace(infosNode.InnerText, "");
                        if (detailValues == "") continue;
                        contextDetails.Add(new DfContextDetailInfo
                        {
                            TagType = HtmlTagName.Span.ToString(),
                            Value = detailValues,
                            ValueType = "TEXT"
                        });
                        break;
                    case HtmlTagName.Pre:
                        imgItem = infosNode.SelectSingleNode(".//img");
                        if (imgItem == null) continue;
                        imgUrl = imgItem.Attributes["src"].Value;
                        contextDetails.Add(new DfContextDetailInfo
                        {
                            TagType = HtmlTagName.Center.ToString(),
                            Value = "",
                            ValueType = "IMG",
                            ResourceUri = imgUrl
                        });
                        break;
                    case HtmlTagName.Div:
                    case HtmlTagName.Br:
                        Log.LogError("[FillContentInfoByHtmlNode] not find div and br info : {} , url : {}",
                            infosNode.InnerHtml, ans.NewsUrl);
                        detailValues = FillWriteLine().Replace(infosNode.InnerText, "");
                        if (detailValues == "") continue;
                        contextDetails.Add(new DfContextDetailInfo
                        {
                            TagType = HtmlTagName.Span.ToString(),
                            Value = detailValues,
                            ValueType = "TEXT"
                        });
                        break;
                    default:
                        continue;
                }
        }

        ans.NewsDataContent = contextDetails;
        var current = new StringBuilder();
        foreach (var detailItem in contextDetails)
            switch (detailItem.ValueType)
            {
                case "TEXT":
                    current.Append(detailItem.Value).Append('\n');
                    break;
                case "TABLE":
                    current.Append(detailItem.Value).Append('\n');
                    break;
                default:
                    continue;
            }

        ans.NewsDataContentText = current.ToString();
        ans.ImgInfos = GetAllContextDetailAndImgInfoList(ans.NewsUrl ?? "", contextDetails);
        return ans;
    }

    // 下载图片信息
    private List<HtmlImageTools.ImgInfo> GetAllContextDetailAndImgInfoList(string baseUrl,
        IReadOnlyList<DfContextDetailInfo> contextDetails)
    {
        var imgUrlList = new List<HtmlImageTools.ImgInfo>();
        foreach (var detailInfo in contextDetails)
            if (detailInfo is { ValueType: "IMG", ResourceUri: not null })
                imgUrlList.Add(new HtmlImageTools.ImgInfo
                {
                    ResourceUrl = detailInfo.ResourceUri,
                    ImgName = HttpUrlTools.GetUrlLastPath(detailInfo.ResourceUri),
                    NewsUrl = baseUrl
                });
        // todo 暂时不下载图片
        // var taskList = imgUrlList.Select(HtmlGetImgDownLoad.DownloadImgAsByteInto).ToList();
        // foreach (var task in taskList) task.Wait();
        // var allImgInfo = taskList.Select(itemTask => itemTask.Result).ToList();
        return imgUrlList;
    }


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
            throw new HtmlFormException(url, $"[GetDfContextInfoByUrl] 拉取详情失败 url : {url} , err : {e}", e);
        }
    }

    public DfContentInfo GetDfContextInfoByHtml(string url, string responseString)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(responseString);
        // 1. 获取 标题 , 信息来源 , 新闻时间
        var htmlNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='contentwrap']");
        if (htmlNode != null) return GetDfContextInfoByNewHtml(url, htmlNode);
        htmlNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='newsContent']");
        if (htmlNode != null) return GetDfContextInfoByOldHtml(url, htmlNode);
        htmlNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='content_text']");
        if (htmlNode != null) return GetDetailInfoByHtml(htmlNode, new DfContentInfo());
        throw new HtmlFormException(url, $"html form err ， url {url}");
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
        return GetDetailInfoByHtml(nowHtmlNode.SelectSingleNode("//div[@id='ContentBody']"), ans);
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
        return GetDetailInfoByHtml(nowHtmlNode.SelectSingleNode("//div[@id='ContentBody']"), ans);
    }
}