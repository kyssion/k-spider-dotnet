using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using k_spider_dotnet.spider.df_news.model;
using k_spider_dotnet.tool.html;
using k_spider_dotnet.tool.http;
using Microsoft.Playwright;

namespace k_spider_dotnet.spider.df_news.playwright;

public partial class DfContextSpiderWithPlaywright(IPlaywright playwright)
{
    public DfContextSpiderWithPlaywright() : this(Playwright.CreateAsync().Result)
    {
    }

    [GeneratedRegex(".(html)")]
    private static partial Regex FillHtml();

    [Obsolete("比较消耗性能 , 暂时不使用playwright 方法拉取")]
    public async Task<DfContentInfo?> GetDfContextInfo(string url, IBrowser? browser, bool useConsole, bool headless)
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
            if (useConsole)
                context.Console += async (_, msg) =>
                {
                    foreach (var arg in msg.Args) Console.WriteLine(await arg.JsonValueAsync<object>());
                };

            var ansList = await GetDfContextInfoWithPlaywrightContext(url, context, useConsole);
            await context.CloseAsync();
            if (!isUseOtherBrowser) await browser.CloseAsync();

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
    private async Task<DfContentInfo?> GetDfContextInfoWithPlaywrightContext(string url, IBrowserContext? context,
        bool useConsole)
    {
        if (context == null) throw new Exception("not find context");

        if (useConsole)
            context.Console += async (_, msg) =>
            {
                foreach (var arg in msg.Args) Console.WriteLine(await arg.JsonValueAsync<object>());
            };

        // 开启一个详细的页面
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(20000);
        var ans = await GetContextInfoData(url, page);
        await page.CloseAsync();
        return ans;
    }

    [Obsolete("比较消耗性能 , 暂时不使用playwright 方法拉取")]
    private async Task<DfContentInfo?> GetContextInfoData(string url, IPage page)
    {
        await page.RouteAsync("**/*", async route =>
        {
            var routerUrl = route.Request.Url;
            if (FillHtml().IsMatch(routerUrl))
                await route.ContinueAsync();
            else
                await route.AbortAsync();
        });
        await page.GotoAsync(url);
        await page.WaitForLoadStateAsync();

        var needDetailInfo = page.Locator("css=.contentwrap").Nth(0);
        if (await needDetailInfo.CountAsync() == 0) return null;

        var ansData = new DfContentInfo
        {
            NewsFrom = url
        };

        ansData.NewsTitle = await needDetailInfo.Locator("css=.title").Nth(0).InnerTextAsync();
        var infosNodeChild = needDetailInfo.Locator("css=div.infos>div.item");
        if (await infosNodeChild.CountAsync() == 0) return null;

        ansData.NewsTime = await infosNodeChild.Nth(0).InnerTextAsync();
        var newsFrom = await infosNodeChild.Nth(1).InnerTextAsync();
        ansData.NewsFrom = newsFrom.Split("：")[1];
        var zwInfo = needDetailInfo.Locator("css=.contentbox").Locator("css=.zwinfos");
        var abstractNode = zwInfo.Locator("css=.abstract");
        if (await abstractNode.CountAsync() != 0)
            ansData.NewsSummary = await abstractNode.Locator("css=.txt").Nth(0).InnerTextAsync();

        var textInfo = await zwInfo.Locator("css=.txtinfos > *").AllAsync();
        ansData.NewsDataContent = [];
        foreach (var textItemInfo in textInfo)
        {
            var nodeType = await textItemInfo.EvaluateAsync("nodeItem => nodeItem.tagName");
            var nodeTypeStr = nodeType.ToString() ?? throw new InvalidOperationException();
            if (!Enum.IsDefined(typeof(HtmlTagName), nodeTypeStr)) continue;

            var valueItem = new DfContextDetailInfo();
            switch ((HtmlTagName)Enum.Parse(typeof(HtmlTagName), nodeTypeStr))
            {
                case HtmlTagName.Div:
                    break;
                case HtmlTagName.Center:
                    var imgItem = textItemInfo.Locator("css=img");
                    if (await imgItem.CountAsync() == 0) continue;

                    var imgUrl = await imgItem.Nth(0).GetAttributeAsync("src") ?? "";
                    valueItem.TagType = nodeTypeStr;
                    valueItem.ValueType = "IMG";
                    valueItem.ResourceUri = imgUrl;
                    ansData.NewsDataContent.Add(valueItem);
                    break;
                case HtmlTagName.P:
                    var className = await textItemInfo.GetAttributeAsync("class");
                    if (className != null) continue;

                    var pImgItem = textItemInfo.Locator("css=img");
                    if (await pImgItem.CountAsync() != 0)
                    {
                        var pImgUrl = await pImgItem.Nth(0).GetAttributeAsync("src") ?? "";
                        valueItem.TagType = nodeTypeStr;
                        valueItem.ValueType = "IMG";
                        valueItem.ResourceUri = pImgUrl;
                        ansData.NewsDataContent.Add(valueItem);
                    }
                    else
                    {
                        valueItem.TagType = nodeTypeStr;
                        valueItem.ValueType = "TEXT";
                        valueItem.Value = await textItemInfo.InnerTextAsync();
                        valueItem.Value = valueItem.Value.Replace(" ", "");
                        if (!valueItem.Value.StartsWith("主力资金加仓名单实时更新")) ansData.NewsDataContent.Add(valueItem);
                    }

                    break;
            }
        }

        // todo 这里处理一下 页面信息的img信息 , 暂时这里不支持
        return ansData;
    }
}