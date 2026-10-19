using System.Net;
using System.Text;

namespace KSpider.Common.Http;

/// <summary>
///     HttpClient 集中管理 : 按主机缓存客户端并统一伪装浏览器请求头。
///     抓取链路不直接调本类 , 一律经 <see cref="KSpider.Spider.Verify.VerifiedHttp" />
///     ( 会话回放与反爬验证重放在其上 ) ; 直接使用方仅限图片下载等无需过验证的场景
/// </summary>
public static class HttpClientTools
{
    static HttpClientTools()
    {
        // 注册 GBK/GB2312 等代码页编码器 : 部分传统站点 ( 同花顺文章列表页 ) 响应头带 charset=gbk ,
        // HttpClient 按 charset 解码需要该 provider , 不注册会静默回退 UTF-8 产生乱码 ( 2026-09-30 实测 )
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    ///     伪装 UA : 公开给浏览器侧复用 ( 见 Spider/Verify 的 BrowserGate ) ——
    ///     过验证时浏览器与 HTTP 请求必须用同一串 UA , 否则指纹不一致会被再拦一次
    /// </summary>
    public const string DisguiseUserAgent =
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";

    // 开启压缩响应自动解压 ( gzip/deflate/br ) , Accept-Encoding 请求头也由 handler 自动携带。
    // 注意 : 不能手动 Add("Accept-Encoding", ...) —— 手动设置后 SocketsHttpHandler 会视为调用方自行处理压缩 , 自动解压随之失效。
    // DecompressionMethods 暂不含 zstd , 请求头也不声明 zstd ( 避免"声明了却解不开"的乱码雷 )。
    // CookieContainer 显式建一个 : handler 的 Cookie 头由它统一拼装 ( 见 ApplyCookies ) , 显式化避免"到底哪个容器在用"的疑问。
    private static readonly HttpClientHandler DecompressHandler = new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.All,
        CookieContainer = new CookieContainer()
    };

    // 不带主机定向请求头的共享客户端 , 供 GetHttpClient 的通用场景 ( 图片下载等 )
    private static readonly HttpClient HttpClient = new HttpClient(DecompressHandler);
    private static readonly Object Lock = new object();
    // host → 独立客户端 : DefaultRequestHeaders 写死了 Host 等主机定向头 , 不能跨主机复用 ;
    // 全部客户端共享 DecompressHandler → CookieContainer 进程内全局共享 , 会话回放依赖这一点
    private static readonly Dictionary<string, HttpClient> HostClientMap = new Dictionary<string, HttpClient>();
    // 使用host创建新的HttpClient
    public static HttpClient CreateByHost(string host)
    {
        lock (Lock)
        {
            if (HostClientMap.TryGetValue(host, out var httpClient))
            {
                return httpClient;
            }
            httpClient = new HttpClient(DecompressHandler);
            httpClient.DefaultRequestHeaders.Add("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            // Accept-Encoding 不在此手动设置 , 由 DecompressHandler 自动携带 ( 见上 )
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
            httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
            httpClient.DefaultRequestHeaders.Add("Host", host);
            httpClient.DefaultRequestHeaders.Add("DNT", "1");
            httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
            httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            httpClient.DefaultRequestHeaders.Add("User-Agent", DisguiseUserAgent);
            httpClient.DefaultRequestHeaders.Add("sec-ch-ua",
                "Chromium;v=\"124\",\"GoogleChrome\";v=\"124\",\"Not-A.Brand\";v=\"99\"");
            httpClient.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
            httpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "macOS");
            HostClientMap.Add(host, httpClient);
            return httpClient;
        }
    }

    // 获取http客户端 
    public static HttpClient GetHttpClient()
    {
        return HttpClient;
    }

    /// <summary>
    ///     把一批 cookie 写进 handler 的 CookieContainer ( 同名旧值先清掉 ) , 供过验证后的会话回放。
    ///     为什么不直接给请求加 Cookie 头 : 容器里已有的同名 cookie 会被追加在手动头之后 ,
    ///     请求会带两份同名 cookie ( 服务端取哪份是未定义行为 ) , 而重复 cookie 本身就是明显的注入指纹。
    ///     统一让容器拼装 Cookie 头 , 会话与容器就不会各说各话 —— 也保住了服务端自己下发的 cookie ( 如 WAF 的 acw_tc ) 的回传。
    /// </summary>
    public static void ApplyCookies(string url, IReadOnlyDictionary<string, string> cookies)
    {
        if (cookies.Count == 0 || !Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;
        var container = DecompressHandler.CookieContainer;
        // 只靠 SetCookies 覆盖不够 : 同名但不同 path 的旧 cookie 会留下来凑成两份 , 先按名字清一遍
        var staleList = container.GetCookies(uri).Cast<Cookie>()
            .Where(cookie => cookies.ContainsKey(cookie.Name)).ToList();
        foreach (var stale in staleList) stale.Expired = true;
        container.SetCookies(uri, string.Join("; ", cookies.Select(item => $"{item.Key}={item.Value}")));
    }
}
