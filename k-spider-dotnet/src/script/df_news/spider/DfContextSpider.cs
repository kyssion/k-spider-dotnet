using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using k_spider_dotnet.tool.html;
using k_spider_dotnet.tool.resource;
using Microsoft.Playwright;
using static System.Text.RegularExpressions.Regex;

namespace k_spider_dotnet.script.df_news.spider;

public partial class DfContextSpider(IPlaywright playwright)
{
    
    [GeneratedRegex(".(html)")]
    private static partial Regex FillHtml();
    [GeneratedRegex(@"\s")]
    private static partial Regex FillWriteLine();
    public DfContextSpider() : this(Playwright.CreateAsync().Result)
    {
    }

    public struct DfContextInfo
    {
        public string? Title { get; set; } // 标题

        public string? AbstractInfo { get; set; } // 摘要

        // 新闻添加时间
        public string? NewsTime { get; set; } // 新闻添加时间
        public string? NewsFrom { get; set; } // 新闻原始来源
        public List<DfContextDetailInfo> DataContext { get; set; } // DataContext 内容序列化结构
        public string? DataContextAll { get; set; } // DataContextAll 内容文本序列化结构
        public string? FromUrl { get; set; } // 新闻原始url
        
        public List<HtmlGetImgDownLoad.ImgInfo> ImgInfos { get; set; } // 新闻原始图片信息
    }

    public struct DfContextDetailInfo
    {
        public string? TagType { get; set; } // 原始结构标签
        public string? Value { get; set; } // 原始结构文本内容
        public string? ValueType { get; set; } // 原始结构 内容类型

        public string? ResourceUri { get; set; } // 如果是图片等资源的 Uri地址
    }

    public async Task<DfContextInfo> GetDfContextInfoByUrl(string url)
    {
        var responseString = await new HttpClient().GetStringAsync(url);
        return this.GetDfContextInfoByHtml(responseString);
    }

    private DfContextInfo GetDfContextInfoByHtml(string responseString)
    {
        var ans = new DfContextInfo();
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(responseString);
        // 1. 获取 标题 , 信息来源 , 新闻时间
        var titleNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='title']");
        ans.Title = titleNode.InnerText;
        var tipBoxNodeChildrens = htmlDoc.DocumentNode.SelectNodes("//div[@class='tipbox']/div[@class='infos']/div");
        ans.NewsTime = FillWriteLine().Replace(tipBoxNodeChildrens[0].InnerText, "");
        ans.NewsFrom = FillWriteLine().Replace(tipBoxNodeChildrens[1].InnerText, "");
        var abstractNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='abstract']/div[@class='txt']");
        ans.AbstractInfo = FillWriteLine().Replace(abstractNode.InnerText, "");
        var txtInfosNodes = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='txtinfos']").ChildNodes;
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
        (ans.DataContextAll , ans.ImgInfos) = this.GetAllContextDetailAndImgInfoList(contextDetails);
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
        foreach (var task in taskList) Task.WaitAll(task);
        var allImgInfo = taskList.Select(itemTask => itemTask.Result).ToList();
        return (allDetailInfos, allImgInfo);
    }

    [Obsolete("比较消耗性能 , 暂时不使用playwright 方法拉取")]
    public async Task<DfContextInfo?> GetDfContextInfo(string url, IBrowser? browser, bool useConsole, bool headless)
    {
        try
        {
            var isUseOtherBrowser = true;
            if (browser == null)
            {
                browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless });
                isUseOtherBrowser = false;
            }

            var context = await browser.NewContextAsync();
            // context 终端能力支持
            if (useConsole == true)
            {
                context.Console += async (_, msg) =>
                {
                    foreach (var arg in msg.Args)
                    {
                        Console.WriteLine(await arg.JsonValueAsync<object>());
                    }
                };
            }

            var ansList = await this.GetDfContextInfoWithPlaywrightContext(url, context, useConsole);
            await context.CloseAsync();
            if (!isUseOtherBrowser)
            {
                await browser.CloseAsync();
            }

            Console.WriteLine(JsonSerializer.Serialize(ansList, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            }));
            return ansList;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [Obsolete("比较消耗性能 , 暂时不使用playwright 方法拉取")]
    private async Task<DfContextInfo?> GetDfContextInfoWithPlaywrightContext(string url, IBrowserContext? context,
        bool useConsole)
    {
        if (context == null)
        {
            throw new Exception("not find context");
        }

        if (useConsole)
        {
            context.Console += async (_, msg) =>
            {
                foreach (var arg in msg.Args)
                {
                    Console.WriteLine(await arg.JsonValueAsync<object>());
                }
            };
        }

        // 开启一个详细的页面
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(20000);
        var ans = await this.GetContextInfoData(url, page);
        await page.CloseAsync();
        return ans;
    }

    [Obsolete("比较消耗性能 , 暂时不使用playwright 方法拉取")]
    private async Task<DfContextInfo?> GetContextInfoData(string url, IPage page)
    {
        await page.RouteAsync("**/*", async route =>
        {
            var routerUrl = route.Request.Url;
            if (FillHtml().IsMatch(routerUrl))
            {
                await route.ContinueAsync();
            }
            else
            {
                await route.AbortAsync();
            }
        });
        await page.GotoAsync(url);
        await page.WaitForLoadStateAsync();

        var needDetailInfo = page.Locator("css=.contentwrap").Nth(0);
        if (await needDetailInfo.CountAsync() == 0)
        {
            return null;
        }

        var ansData = new DfContextInfo()
        {
            FromUrl = url
        };

        ansData.Title = await needDetailInfo.Locator("css=.title").Nth(0).InnerTextAsync();
        var infosNodeChild = needDetailInfo.Locator("css=div.infos>div.item");
        if (await infosNodeChild.CountAsync() == 0)
        {
            return null;
        }

        ansData.NewsTime = await infosNodeChild.Nth(0).InnerTextAsync();
        var newsFrom = await infosNodeChild.Nth(1).InnerTextAsync();
        ansData.NewsFrom = newsFrom.Split("：")[1];
        var zwInfo = needDetailInfo.Locator("css=.contentbox").Locator("css=.zwinfos");
        var abstractNode = zwInfo.Locator("css=.abstract");
        if (await abstractNode.CountAsync() != 0)
        {
            ansData.AbstractInfo = await abstractNode.Locator("css=.txt").Nth(0).InnerTextAsync();
        }

        var textInfo = await zwInfo.Locator("css=.txtinfos > *").AllAsync();
        ansData.DataContext = [];
        foreach (var textItemInfo in textInfo)
        {
            var nodeType = await textItemInfo.EvaluateAsync("nodeItem => nodeItem.tagName");
            var nodeTypeStr = nodeType.ToString() ?? throw new InvalidOperationException();
            if (!Enum.IsDefined(typeof(HtmlTagName), nodeTypeStr))
            {
                continue;
            }

            var valueItem = new DfContextDetailInfo();
            switch ((HtmlTagName)Enum.Parse(typeof(HtmlTagName), nodeTypeStr))
            {
                case HtmlTagName.Div:
                    break;
                case HtmlTagName.Center:
                    var imgItem = textItemInfo.Locator("css=img");
                    if (await imgItem.CountAsync() == 0)
                    {
                        continue;
                    }

                    var imgUrl = await imgItem.Nth(0).GetAttributeAsync("src") ?? "";
                    valueItem.TagType = nodeTypeStr;
                    valueItem.ValueType = "IMG";
                    valueItem.ResourceUri = imgUrl;
                    ansData.DataContext.Add(valueItem);
                    break;
                case HtmlTagName.P:
                    var className = await textItemInfo.GetAttributeAsync("class");
                    if (className != null)
                    {
                        continue;
                    }

                    var pImgItem = textItemInfo.Locator("css=img");
                    if (await pImgItem.CountAsync() != 0)
                    {
                        var pImgUrl = await pImgItem.Nth(0).GetAttributeAsync("src") ?? "";
                        valueItem.TagType = nodeTypeStr;
                        valueItem.ValueType = "IMG";
                        valueItem.ResourceUri = pImgUrl;
                        ansData.DataContext.Add(valueItem);
                    }
                    else
                    {
                        valueItem.TagType = nodeTypeStr;
                        valueItem.ValueType = "TEXT";
                        valueItem.Value = await textItemInfo.InnerTextAsync();
                        valueItem.Value = valueItem.Value.Replace(" ", "");
                        if (!valueItem.Value.StartsWith("主力资金加仓名单实时更新"))
                        {
                            ansData.DataContext.Add(valueItem);
                        }
                    }

                    break;
                default:
                    break;
            }
        }

        // todo 这里处理一下 页面信息的img信息 , 暂时这里不支持
        return ansData;
    }
}