using System.Globalization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using k_spider_dotnet.script.df_news.spider.model;
using k_spider_dotnet.tool.http;
using k_spider_dotnet.tool.log;
using k_spider_dotnet.tool.resource;
using Microsoft.Extensions.Logging;

namespace k_spider_dotnet.script.df_news.spider;

public partial class DfContextSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<DfListSpider>();
    
    [GeneratedRegex(@"\s")]
    private static partial Regex FillWriteLine();
    public async Task<DfContextInfo> GetDfContextInfoByUrl(string url)
    {
        try
        {
            var responseString = await new HttpClient().GetStringAsync(url);
            return this.GetDfContextInfoByHtml(url ,responseString);
        }
        catch (Exception e)
        {
            Log.LogError("[GetDfContextInfoByUrl] 拉取详情失败 url : {} , err : {}", url, e);
            throw;
        }
    }

    // todo 有一个特殊的文档  https://fund.eastmoney.com/a/1593,202404223054066289.html
    private DfContextInfo GetDfContextInfoByHtml(string url , string responseString)
    {
        var ans = new DfContextInfo
        {
            FromUrl = url
        };
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(responseString);
        // 1. 获取 标题 , 信息来源 , 新闻时间
        var htmlNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='contentwrap']");
        if (htmlNode!= null)
        {
            return GetDfContextInfoByNewHtml(url, htmlNode);
        }
        htmlNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='newsContent']");
        if (htmlNode!= null)
        {
            return GetDfContextInfoByOldHtml(url, htmlNode);
        }

        throw new Exception($"当前html 格式暂时没有支持 ， url {url}");
    }
    private DfContextInfo GetDfContextInfoByOldHtml(string url ,HtmlNode nowHtmlNode)
    {
        var ans = new DfContextInfo
        {
            FromUrl = url
        };
        var titleNode = nowHtmlNode.SelectSingleNode("//h1");
        ans.Title = titleNode.InnerText;
        ans.NewsTime = FillWriteLine().Replace(nowHtmlNode.SelectSingleNode("//div[@class='time']").InnerText, "");
        ans.NewsFrom = FillWriteLine().Replace(nowHtmlNode.SelectSingleNode("//div[@class='source']").InnerText, "");
        ans.AbstractInfo = FillWriteLine().Replace(nowHtmlNode.SelectSingleNode("//div[@class='b-review']")?.InnerText??"", "");
        var txtInfosNodes = nowHtmlNode.SelectSingleNode("//div[@id='ContentBody']").ChildNodes;
        var contextDetails = new List<DfContextDetailInfo>();
        var ti = CultureInfo.CurrentCulture.TextInfo;
        foreach (var infosNode in txtInfosNodes)
        {
            if (infosNode.NodeType != HtmlNodeType.Text && infosNode.NodeType != HtmlNodeType.Comment)
            {
                switch ((HtmlTagName)Enum.Parse(typeof(HtmlTagName), ti.ToTitleCase(infosNode.Name)))
                {
                    case HtmlTagName.Center:
                        var imgItem = infosNode.SelectSingleNode("./img");
                        if (imgItem == null)
                        {
                            continue;
                        }
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
                        if (infosNode.Attributes["class"] != null)
                        {
                            continue;
                        }

                        var pImgItem = infosNode.SelectSingleNode("./img");
                        if (pImgItem!=null)
                        {
                            var pImgItemUrl = pImgItem.Attributes["src"].Value;
                            contextDetails.Add(new DfContextDetailInfo
                            {
                                TagType = HtmlTagName.P.ToString(),
                                Value = "",
                                ValueType = "IMG",
                                ResourceUri =pImgItemUrl
                            });
                        }
                        else
                        {
                            var detailValues = FillWriteLine().Replace(infosNode.InnerText, "");
                            if (detailValues.StartsWith("主力资金加仓名单实时更新"))
                            {
                                continue;
                            }
                            contextDetails.Add(new DfContextDetailInfo
                            {
                                TagType = HtmlTagName.P.ToString(),
                                Value = detailValues,
                                ValueType = "TEXT",
                            });
                        }
                        break;
                    case HtmlTagName.Div:
                    case HtmlTagName.H1:
                    default:
                        continue;
                }
            }
        }
        ans.DataContext = contextDetails;
        (ans.DataContextAll , ans.ImgInfos) = this.GetAllContextDetailAndImgInfoList(contextDetails);
        return ans;
    }

    private DfContextInfo GetDfContextInfoByNewHtml(string url ,HtmlNode nowHtmlNode)
    {
        var ans = new DfContextInfo
        {
            FromUrl = url
        };
        var titleNode = nowHtmlNode.SelectSingleNode("//div[@class='title']");
        ans.Title = titleNode.InnerText;
        var tipBoxNodeChildNodes= nowHtmlNode.SelectNodes("//div[@class='tipbox']/div[@class='infos']/div");
        ans.NewsTime = FillWriteLine().Replace(tipBoxNodeChildNodes[0].InnerText, "");
        ans.NewsFrom = FillWriteLine().Replace(tipBoxNodeChildNodes[1].InnerText, "");
        ans.AbstractInfo = FillWriteLine().Replace(nowHtmlNode.SelectSingleNode("//div[@class='abstract']/div[@class='txt']")?.InnerText??"", "");
        var txtInfosNodes = nowHtmlNode.SelectSingleNode("//div[@class='txtinfos']").ChildNodes;
        var contextDetails = new List<DfContextDetailInfo>();
        var ti = CultureInfo.CurrentCulture.TextInfo;
        foreach (var infosNode in txtInfosNodes)
        {
            if (infosNode.NodeType != HtmlNodeType.Text && infosNode.NodeType != HtmlNodeType.Comment)
            {
                switch ((HtmlTagName)Enum.Parse(typeof(HtmlTagName), ti.ToTitleCase(infosNode.Name)))
                {
                    case HtmlTagName.Center:
                        var imgItem = infosNode.SelectSingleNode("./img");
                        if (imgItem == null)
                        {
                            continue;
                        }
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
                        if (infosNode.Attributes["class"] != null)
                        {
                            continue;
                        }

                        var pImgItem = infosNode.SelectSingleNode("./img");
                        if (pImgItem!=null)
                        {
                            var pImgItemUrl = pImgItem.Attributes["src"].Value;
                            contextDetails.Add(new DfContextDetailInfo
                            {
                                TagType = HtmlTagName.P.ToString(),
                                Value = "",
                                ValueType = "IMG",
                                ResourceUri =pImgItemUrl
                            });
                        }
                        else
                        {
                            var detailValues = FillWriteLine().Replace(infosNode.InnerText, "");
                            if (detailValues.StartsWith("主力资金加仓名单实时更新"))
                            {
                                continue;
                            }
                            contextDetails.Add(new DfContextDetailInfo
                            {
                                TagType = HtmlTagName.P.ToString(),
                                Value = detailValues,
                                ValueType = "TEXT",
                            });
                        }
                        break;
                    case HtmlTagName.Div:
                    default:
                        continue;
                }
            }
        }
        ans.DataContext = contextDetails;
        (ans.DataContextAll , ans.ImgInfos) = GetAllContextDetailAndImgInfoList(contextDetails);
        return ans;
    }

    // 下载图片信息
    private (string allDetailInfos, List<HtmlGetImgDownLoad.ImgInfo> allImgInfo) GetAllContextDetailAndImgInfoList(IReadOnlyList<DfContextDetailInfo> contextDetails)
    {
        var allDetailInfos = "";
        var imgUrlList = new Dictionary<int,string>();
        for(var a=0;a<contextDetails.Count;a++)
        {
            var detailInfo = contextDetails[a];
            if (detailInfo is { ValueType: "IMG", ResourceUri: not null })
            {
                imgUrlList.Add(a,detailInfo.ResourceUri);
            }
            if (detailInfo.ValueType == "TEXT")
            {
                allDetailInfos += detailInfo.Value + "\n";
            }
        }
        var taskList = imgUrlList.Select(keyValue => HtmlGetImgDownLoad.DownloadImgAsByte(keyValue.Value, "" + keyValue.Key)).ToList();
        foreach (var task in taskList) task.Wait();
        var allImgInfo = taskList.Select(itemTask => itemTask.Result).ToList();
        return (allDetailInfos, allImgInfo);
    }
}