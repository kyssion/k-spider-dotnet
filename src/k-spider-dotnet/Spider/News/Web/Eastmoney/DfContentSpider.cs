using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using KSpider.Exceptions;
using KSpider.Spider.News.Web;
using KSpider.Spider.Verify;
using KSpider.Common.Html;
using KSpider.Common.Http;
using KSpider.Common;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.News.Web.Eastmoney;

/// <summary>
///     东方财富新闻详情抓取与解析 : 现行管线走详情接口 ( GetDfContentOriginInfoByInterface + GetContentInfoByJson ) ,
///     GetDfContextInfoByUrl 一族是直接抓详情页 HTML 的路径 , 当前无调用方保留备用
/// </summary>
public partial class DfContentSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<DfContentSpider>();

    // 清洗文本 : 去除全部空白字符
    [GeneratedRegex(@"\s")]
    private static partial Regex FillWriteLine();

    /// <summary>
    ///     从新闻页 URL 提取详情接口的文章参数
    ///     真实 URL 有两种形态 : /news/1345,202609183878840472.html 与 /a/202609061234567.html
    /// </summary>
    public static string GetArticleParamFromUrl(string url)
    {
        var lastPath = HttpUrlTools.GetUrlLastPath(url);
        return lastPath.Split(".", 2)[0].Split(",", 2)[^1];
    }

    /// <summary>
    ///     详情接口拉取原始内容 : 失败不抛异常 , 填 Failed 状态与 Message 交回状态机重试
    /// </summary>
    public async Task<DfNewsContentOrigin> GetDfContentOriginInfoByInterface(string url)
    {
        var paramsNumber = GetArticleParamFromUrl(url);
        var ans = new DfNewsContentOrigin
        {
            NewsUrl = url,
            OriginType = NewsContentOriginType.Json,
            NewsOriginContent = ""
        };
        var newUrl = string.Format(DfNewsResource.RequestDfContextUrl, paramsNumber,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        try
        {
            var responseString = await VerifiedHttp.GetStringAsync(DfNewsResource.ContextApiResourceHost, newUrl);
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

    /// <summary>
    ///     详情接口一步到位 ( 下载 + 解析 ) : 下载异常原样上抛 , 其余异常包装为 HtmlFormException
    /// </summary>
    public async Task<DfContentInfo> GetDfContextInfoByUrlInterface(string url)
    {
        var paramsNumber = GetArticleParamFromUrl(url);
        var newUrl = string.Format(DfNewsResource.RequestDfContextUrl, paramsNumber,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        try
        {
            return GetContentInfoByJson(
                await VerifiedHttp.GetStringAsync(DfNewsResource.ContextApiResourceHost, newUrl), url);
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

    /// <summary>
    ///     解析详情接口 JSON 响应为 DfContentInfo , 正文 HTML 再交 GetDetailInfoByHtml 拆片段
    ///     ( 独立成公开方法供离线测试 )
    /// </summary>
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
            // 摘要取导语字段 Art_Guidance , 不是标题 ( 此前误读 Art_Title , 摘要恒等于标题 )
            ans.NewsSummary = jsonData["Art_Guidance"]?.ToString() ?? "";
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

    /// <summary>
    ///     正文 HTML 逐节点拆为 NewsContentSegment : 已知名签按语义归类 , 未知标签跳过片段并记日志
    /// </summary>
    private DfContentInfo GetDetailInfoByHtml(HtmlNode htmlDoc, DfContentInfo ans)
    {
        var txtInfNodes = htmlDoc.ChildNodes;
        var contextDetails = new List<NewsContentSegment>();
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
                contextDetails.Add(new NewsContentSegment
                {
                    TagType = HtmlTagName.Table.ToString(),
                    Value = infosNode.InnerText,
                    ValueType = NewsContentSegment.TextType
                });
                continue;
            }

            if (infosNode.NodeType != HtmlNodeType.Comment)
            {
                // 未知标签 ( iframe/figure 等 ) 跳过该片段并记日志 ,
                // 防止源站新增标签导致整篇新闻解析失败、重试耗尽后静默丢正文
                if (!Enum.TryParse<HtmlTagName>(ti.ToTitleCase(infosNode.Name), true, out var tagName) ||
                    !Enum.IsDefined(typeof(HtmlTagName), tagName))
                {
                    Log.LogWarning("[FillContentInfoByHtmlNode] unknown html tag skip , tag : {} , url : {}",
                        infosNode.Name, ans.NewsUrl);
                    continue;
                }

                switch (tagName)
                {
                    case HtmlTagName.Strong:
                        var strongImage = infosNode.SelectSingleNode(".//img");
                        if (strongImage == null)
                        {
                            detailValues = FillWriteLine().Replace(infosNode.InnerText, "");
                            if (detailValues == "") continue;
                            contextDetails.Add(new NewsContentSegment
                            {
                                TagType = HtmlTagName.Table.ToString(),
                                Value = infosNode.InnerText,
                                ValueType = NewsContentSegment.OtherType
                            });
                            continue;
                        }

                        var strongUrl = strongImage.Attributes["src"].Value;
                        contextDetails.Add(new NewsContentSegment
                        {
                            TagType = HtmlTagName.Center.ToString(),
                            Value = "",
                            ValueType = NewsContentSegment.ImgType,
                            ResourceUri = strongUrl
                        });
                        break;
                    case HtmlTagName.Ul:
                        var liNodes = infosNode.SelectNodes("li") ?? infosNode.SelectNodes("ul/li");
                        if (liNodes == null)
                        {
                            detailValues = FillWriteLine().Replace(infosNode.InnerText, "");
                            if (detailValues == "") continue;
                            contextDetails.Add(new NewsContentSegment
                            {
                                TagType = HtmlTagName.Table.ToString(),
                                Value = infosNode.InnerText,
                                ValueType = NewsContentSegment.OtherType
                            });
                            continue;
                        }

                        if (liNodes.Count == 0) continue;
                        var liList = liNodes.Select(
                            liNode => liNode.InnerText
                        ).ToList();
                        contextDetails.Add(new NewsContentSegment
                        {
                            TagType = HtmlTagName.Table.ToString(),
                            Value = JsonTools.GetJson(liList),
                            ValueType = NewsContentSegment.UlType
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
                        contextDetails.Add(new NewsContentSegment
                        {
                            TagType = HtmlTagName.Table.ToString(),
                            Value = JsonTools.GetJson(tableDataList),
                            ValueType = NewsContentSegment.TableType
                        });
                        break;
                    case HtmlTagName.Center:
                        var imgItem = infosNode.SelectSingleNode(".//img");
                        if (imgItem == null) continue;
                        var imgUrl = imgItem.Attributes["src"].Value;
                        contextDetails.Add(new NewsContentSegment
                        {
                            TagType = HtmlTagName.Center.ToString(),
                            Value = "",
                            ValueType = NewsContentSegment.ImgType,
                            ResourceUri = imgUrl
                        });
                        break;
                    case HtmlTagName.P:
                        if (infosNode.Attributes["class"] != null) continue;
                        var pImgItem = infosNode.SelectSingleNode(".//img");
                        if (pImgItem != null)
                        {
                            var pImgItemUrl = pImgItem.Attributes["src"].Value;
                            contextDetails.Add(new NewsContentSegment
                            {
                                TagType = HtmlTagName.P.ToString(),
                                Value = "",
                                ValueType = NewsContentSegment.ImgType,
                                ResourceUri = pImgItemUrl
                            });
                        }
                        else
                        {
                            detailValues = FillWriteLine().Replace(infosNode.InnerText, "");
                            if (detailValues == "" || detailValues.StartsWith("主力资金加仓名单实时更新")) continue;
                            contextDetails.Add(new NewsContentSegment
                            {
                                TagType = HtmlTagName.P.ToString(),
                                Value = detailValues,
                                ValueType = NewsContentSegment.TextType
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
                        contextDetails.Add(new NewsContentSegment
                        {
                            TagType = HtmlTagName.H.ToString(),
                            Value = detailValues,
                            ValueType = NewsContentSegment.TextType
                        });
                        break;
                    case HtmlTagName.Span:
                        detailValues = FillWriteLine().Replace(infosNode.InnerText, "");
                        if (detailValues == "") continue;
                        contextDetails.Add(new NewsContentSegment
                        {
                            TagType = HtmlTagName.Span.ToString(),
                            Value = detailValues,
                            ValueType = NewsContentSegment.TextType
                        });
                        break;
                    case HtmlTagName.Pre:
                        imgItem = infosNode.SelectSingleNode(".//img");
                        if (imgItem == null) continue;
                        imgUrl = imgItem.Attributes["src"].Value;
                        contextDetails.Add(new NewsContentSegment
                        {
                            TagType = HtmlTagName.Center.ToString(),
                            Value = "",
                            ValueType = NewsContentSegment.ImgType,
                            ResourceUri = imgUrl
                        });
                        break;
                    case HtmlTagName.Div:
                    case HtmlTagName.Br:
                        Log.LogError("[FillContentInfoByHtmlNode] not find div and br info : {} , url : {}",
                            infosNode.InnerHtml, ans.NewsUrl);
                        detailValues = FillWriteLine().Replace(infosNode.InnerText, "");
                        if (detailValues == "") continue;
                        contextDetails.Add(new NewsContentSegment
                        {
                            TagType = HtmlTagName.Span.ToString(),
                            Value = detailValues,
                            ValueType = NewsContentSegment.TextType
                        });
                        break;
                    default:
                        continue;
                }
            }
        }

        ans.NewsDataContent = contextDetails;
        var current = new StringBuilder();
        foreach (var detailItem in contextDetails)
            switch (detailItem.ValueType)
            {
                case NewsContentSegment.TextType:
                    current.Append(detailItem.Value).Append('\n');
                    break;
                case NewsContentSegment.TableType:
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
        IReadOnlyList<NewsContentSegment> contextDetails)
    {
        var imgUrlList = new List<HtmlImageTools.ImgInfo>();
        foreach (var detailInfo in contextDetails)
            if (detailInfo is { ValueType: NewsContentSegment.ImgType, ResourceUri: not null })
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


    /// <summary>
    ///     直接抓详情页 HTML 并解析 ( 非 JSON 接口路径 , 当前无调用方 )
    /// </summary>
    public async Task<DfContentInfo> GetDfContextInfoByUrl(string url)
    {
        try
        {
            // 页面 URL 域名不固定 ( finance/hk/stock 等站点 ) , 使用无伪装头的共享客户端
            var responseString = await HttpClientTools.GetHttpClient().GetStringAsync(url);
            return GetDfContextInfoByHtml(url, responseString);
        }
        catch (Exception e)
        {
            Log.LogError("[GetDfContextInfoByUrl] 拉取详情失败 url : {} , err : {}", url, e);
            throw new HtmlFormException(url, $"[GetDfContextInfoByUrl] 拉取详情失败 url : {url} , err : {e}", e);
        }
    }

    /// <summary>
    ///     详情页 HTML 解析入口 : 按新版 / 旧版模板选择器分流 , 都匹配不上抛 HtmlFormException
    /// </summary>
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
