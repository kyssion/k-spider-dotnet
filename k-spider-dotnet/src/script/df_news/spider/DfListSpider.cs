using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using k_spider_dotnet.tool.http;
using k_spider_dotnet.tool.resource;
using Microsoft.Playwright;

namespace k_spider_dotnet.script.df_news.spider;

public class DfListSpider(IPlaywright playwright)
{
    private static readonly Regex Regex = new Regex(".(png|jpg|css|aspx|ico)");

    public struct DfListInfo
    {
        public string NewsUrl { get; set; }
        public string NewsTitle { get; set; }
        public string NewsSummary { get; set; }
        public string NewsTime { get; set; }
        public string NewsFrom { get; set; }
        public NewsFromType FromMedia { get; set; }
        public DateTime NewsDownloadTime { get; set; }
    }

    public DfListSpider() : this(Playwright.CreateAsync().Result)
    {
    }

    public async Task<List<DfListInfo>> GetDfListInfoByUrl(int dfModelNumber, int pageStartNumber,
        int pageEndNumber, int pageSize, DfListOrderType orderType)
    {
        var nowTime = DateTime.Now.Millisecond;
        var ans = new List<DfListInfo>();
        while (pageStartNumber <= pageEndNumber)
        {
            ans.AddRange(await GetDfListInfoByUrl(dfModelNumber, pageStartNumber, pageSize, orderType));
            pageStartNumber++;
        }
        return ans;
    }


    private async Task<List<DfListInfo>> GetDfListInfoByUrl(int dfModelNumber, int pageNumber, int pageSize,
        DfListOrderType orderType)
    {
        var urlNow = string.Format(DfResource.RequestDfListUrl, dfModelNumber, (int)orderType, pageNumber, pageSize,
            DateTime.Now.Millisecond);
        var responseString = await HttpClientTool.Create(DfResource.ListResourceHost).GetStringAsync(urlNow);

        var forecastNode = JsonNode.Parse(responseString)!;
        var jsonData = forecastNode["data"];
        if (jsonData?["list"] == null)
        {
            throw new Exception("not find date");
        }

        var jsonDataList = (JsonArray)jsonData["list"]!;

        var ans = jsonDataList.OfType<JsonNode>()
            .Select(dataItem => new DfListInfo
            {
                NewsUrl = dataItem["url"]!.ToString(),
                NewsTitle = dataItem["title"]!.ToString(),
                NewsSummary = dataItem["summary"]!.ToString(),
                NewsTime = dataItem["showTime"]!.ToString(),
                FromMedia =NewsFromType.DfMedia,
                NewsFrom = dataItem["mediaName"]!.ToString(),
                NewsDownloadTime = DateTime.Now,
            })
            .ToList();
        return ans;
    }

    public async Task<List<List<DfListInfo>>> GetDfListInfo(string url, int pageNum, IBrowser? browser, bool useConsole,
        bool headless)
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

            var ansList = await this.GetListInfoWithPlaywright(url, pageNum, context);
            await context.CloseAsync();
            if (!isUseOtherBrowser)
            {
                await browser.CloseAsync();
            }
            return ansList;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task<List<List<DfListInfo>>> GetListInfoWithPlaywright(string baseUrl, int pageNum,
        IBrowserContext context)
    {
        var ans = new List<List<DfListInfo>>();
        for (var i = 0; i < pageNum; i++)
        {
            var urlNow = string.Format(baseUrl, i + 1);
            ans.Add(await this.GetListContentDataInfo(urlNow, context));
        }

        return ans;
    }

    private async Task<List<DfListInfo>> GetListContentDataInfo(string urlNow, IBrowserContext context)
    {
        var page = await context.NewPageAsync();
        await page.RouteAsync("**/*", async route =>
        {
            var routerUrl = route.Request.Url;
            if (!DfListSpider.Regex.IsMatch(routerUrl))
            {
                await route.ContinueAsync();
            }
            else
            {
                await route.AbortAsync();
            }
        });
        await page.GotoAsync(urlNow);
        await page.WaitForLoadStateAsync();
        var evaluateResult = await page.EvaluateAsync<object>("""
                                                                      () =>{
                                                                          let ans = [];
                                                                          let item = document.querySelector("#newsListContent")
                                                                          if (item == null) {
                                                                              return ans
                                                                          }
                                                                          let childLi = item.getElementsByTagName("li")
                                                                          if (childLi) {
                                                                              for (let li of childLi) {
                                                                                  let textDiv = li.getElementsByClassName("text")[0];
                                                                                  if (textDiv) {
                                                                                      let title = textDiv.getElementsByClassName("title")[0];
                                                                                      let info = textDiv.getElementsByClassName("info")[0];
                                                                                      let timeNode = textDiv.getElementsByClassName("time")[0];
                                                                                      if (title && info) {
                                                                                          let titleA = title.getElementsByTagName("a")[0];
                                                                                          let dataUrl = titleA.href;
                                                                                          let dataTitle = titleA.innerHTML;
                                                                                          let dataInfo = info.getAttribute("title") ?? "";
                                                                                          let dataTime = timeNode.innerHTML
                                                                                          if (dataTitle) {
                                                                                              ans.push({
                                                                                                  url: dataUrl,
                                                                                                  title: dataTitle,
                                                                                                  summary: dataInfo,
                                                                                                  time: dataTime
                                                                                              })
                                                                                          }
                                                                                      }
                                                                                  }
                                                                              }
                                                                          }
                                                                          return ans
                                                                      }
                                                              """);
        await page.CloseAsync();
        var pageInfos = (object[])evaluateResult;
        return (from IDictionary<string, object>? item in pageInfos
            select new DfListInfo
            {
                NewsUrl = item["url"].ToString(),
                NewsTitle = item["title"].ToString(),
                NewsSummary = item["summary"].ToString(),
                NewsTime = item["time"].ToString(),
            }).ToList();
    }
}