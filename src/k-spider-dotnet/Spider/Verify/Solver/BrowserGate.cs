using KSpider.Tool.Http;
using Microsoft.Playwright;

namespace KSpider.Spider.Verify.Solver;

/// <summary>
///     浏览器会话的公共部分 : 启动 Chromium ( UA / platform / languages 与 HTTP 链路一致 ) 、打开目标地址、导出 cookie。
///     两个浏览器策略 ( JS 挑战 / 滑块 ) 共用它 , 差别只在"打开之后怎么过验证"。
///     浏览器只在被拦截时按需启动 , 生产链路仍然只用 HTTP。
/// </summary>
internal sealed class BrowserGate : IAsyncDisposable
{
    private const int BrowserTimeoutMs = 30000;

    /// <summary>
    ///     抹掉与伪装 UA 自相矛盾的自动化特征。headless 默认三处露馅 :
    ///     navigator.webdriver 为 true 、platform 报宿主系统 ( Win32/Linux ) 而 UA 声称 macOS 、
    ///     languages 只有一项而 HTTP 的 Accept-Language 声明了 zh-CN,zh,en —— 指纹检测对这三处比对很直接。
    ///     这是"降低自动化特征"而不是完整指纹伪装 : 插件数、WebGL 渲染器、字体等仍可能与真机不同 ( 见文档已知限制 ) 。
    /// </summary>
    private const string FingerprintScript = """
        Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
        Object.defineProperty(navigator, 'platform', { get: () => 'MacIntel' });
        Object.defineProperty(navigator, 'languages', { get: () => ['zh-CN', 'zh', 'en'] });
        if (!window.chrome) { window.chrome = { runtime: {} }; }
        """;

    private readonly IBrowser _browser;
    private readonly IBrowserContext _context;
    private readonly IPlaywright _playwright;

    private BrowserGate(IPlaywright playwright, IBrowser browser, IBrowserContext context, IPage page)
    {
        _playwright = playwright;
        _browser = browser;
        _context = context;
        Page = page;
    }

    public IPage Page { get; }

    public async ValueTask DisposeAsync()
    {
        // 收尾失败没有补救价值 , 且不能让它盖住真正的处理结果
        try
        {
            await _context.CloseAsync();
        }
        catch
        {
            // ignore
        }

        try
        {
            await _browser.CloseAsync();
        }
        catch
        {
            // ignore
        }

        _playwright.Dispose();
    }

    public static async Task<BrowserGate> OpenAsync(string url, CancellationToken cancellationToken)
    {
        // 已经超时 / 被取消时不必再起浏览器 ( Playwright 的导航没有 CancellationToken 重载 , 只能在这里先挡一次 )
        cancellationToken.ThrowIfCancellationRequested();
        // 全限定库入口 : 本解决方案里存在 Spider/News/Web/Eastmoney/Playwright 命名空间 ( AGENTS 同款约定 )
        var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        IBrowser? browser = null;
        IBrowserContext? context = null;
        try
        {
            browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                // 走完整 Chromium 的新 headless : 默认的 chrome-headless-shell ( 旧 headless ) 是最容易被识别的一档
                Channel = "chromium",
                Args = ["--disable-blink-features=AutomationControlled"]
            });
            // UA 与 HttpClientTools 的伪装头保持一致 : 浏览器过完挑战种下的 cookie ,
            // 随后要能被同一 UA 的 HTTP 请求直接复用 , 两边指纹不一致会被重新拦一次
            context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = HttpClientTools.DisguiseUserAgent,
                Locale = "zh-CN",
                TimezoneId = "Asia/Shanghai",
                ViewportSize = new ViewportSize { Width = 1440, Height = 900 }
            });
            // 初始化脚本要在建页面之前挂上 , 否则第一份文档里的 navigator 还是原始值
            await context.AddInitScriptAsync(FingerprintScript);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(BrowserTimeoutMs);
            await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = BrowserTimeoutMs
            });
            return new BrowserGate(playwright, browser, context, page);
        }
        catch
        {
            // 打开失败要把已起的浏览器收掉 , 否则一次失败就漏一个进程
            if (context != null) await context.CloseAsync();
            if (browser != null) await browser.CloseAsync();
            playwright.Dispose();
            throw;
        }
    }

    /// <summary>当前页面 DOM 序列化结果 , 供识别器判断挑战是否已经解除</summary>
    public async Task<string> ContentAsync()
    {
        return await Page.ContentAsync();
    }

    /// <summary>
    ///     导出目标主机自己的 cookie ( 含挑战脚本种下的放行 cookie )。
    ///     按域过滤 : 上下文里还有挑战域的第三方 cookie ( 如 challenges.cloudflare.com ) ,
    ///     把它们发给目标主机既没用 , 也是一眼可疑的请求。
    /// </summary>
    public async Task<IReadOnlyDictionary<string, string>> ExportCookiesAsync(string host)
    {
        var cookies = await _context.CookiesAsync();
        return cookies
            .Where(cookie => !string.IsNullOrEmpty(cookie.Name) && DomainMatches(cookie.Domain, host))
            .GroupBy(cookie => cookie.Name)
            .ToDictionary(group => group.Key, group => group.Last().Value);
    }

    /// <summary>cookie 的 Domain 可能是主机名本身 , 也可能是上级域 ( 形如 .cls.cn )</summary>
    private static bool DomainMatches(string? cookieDomain, string host)
    {
        if (string.IsNullOrEmpty(cookieDomain)) return false;
        var domain = cookieDomain.TrimStart('.');
        return host.Equals(domain, StringComparison.OrdinalIgnoreCase)
               || host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>导出 cookie 与识别器重跑共用的探针构造 , 避免两处各写一遍</summary>
    public static VerificationProbe BuildProbe(string url, string body)
    {
        return new VerificationProbe(url, 200, body);
    }
}
