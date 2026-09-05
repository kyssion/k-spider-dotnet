using System.Text.RegularExpressions;
using k_spider_dotnet.spider.df_news.model;
using k_spider_dotnet.tool.http;
using Microsoft.Playwright;

namespace k_spider_dotnet.spider.df_news.playwright;

public class DfListSpiderWithPlaywright
{
    private static readonly Regex ImageRegex = new(@".(\.png|\.jpg|\.css|\.aspx|\.ico)");

    private readonly IBrowser _browser;
    private readonly IBrowserContext _context;

    public DfListSpiderWithPlaywright(bool headless, bool useConsole)
    {
        var playwright = Playwright.CreateAsync().Result;
        _browser = playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless }).Result;
        _context = _browser.NewContextAsync().Result;
        // context 终端能力支持
        if (useConsole)
            _context.Console += async (_, msg) =>
            {
                foreach (var arg in msg.Args) Console.WriteLine(await arg.JsonValueAsync<object>());
            };
    }

    public async Task Close()
    {
        await _context.CloseAsync();
        await _browser.CloseAsync();
    }

    public async Task<int> GetListResourceNumberInfo(string url)
    {
        var page = await _context.NewPageAsync();
        try
        {
            await page.RouteAsync("**/*", async route =>
            {
                var routerUrl = route.Request.Url;
                if (!ImageRegex.IsMatch(routerUrl))
                    await route.ContinueAsync();
                else
                    await route.AbortAsync();
            });
            var waitForRequestTask = page.WaitForRequestAsync("**/getNewsByColumns*");
            await page.GotoAsync(url);
            var request = await waitForRequestTask;
            var paramsList = HttpUrlTools.GetUrlParamInfo(request.Url, "column");
            if (paramsList.Length == 0) throw new Exception("getNewsByColumns , column not find");

            return int.Parse(paramsList[0]);
        }
        finally
        {
            await page.CloseAsync();
        }
    }


    public async Task<List<List<DfListInfo>>> GetDfListInfo(string url, int pageNum)
    {
        try
        {
            var ansList = await GetListInfoWithPlaywright(url, pageNum, _context);
            await _context.CloseAsync();
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
            ans.Add(await GetListContentDataInfo(urlNow, context));
        }

        return ans;
    }

    private async Task<List<DfListInfo>> GetListContentDataInfo(string urlNow, IBrowserContext context)
    {
        var page = await context.NewPageAsync();
        await page.RouteAsync("**/*", async route =>
        {
            var routerUrl = route.Request.Url;
            if (!ImageRegex.IsMatch(routerUrl))
                await route.ContinueAsync();
            else
                await route.AbortAsync();
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
                NewsUrl = item["url"].ToString() ?? "",
                NewsTitle = item["title"].ToString() ?? "",
                NewsSummary = item["summary"].ToString() ?? "",
                NewsTime = item["time"].ToString() ?? ""
            }).ToList();
    }
}