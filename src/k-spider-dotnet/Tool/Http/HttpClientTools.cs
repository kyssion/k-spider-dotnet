using System.Net;

namespace KSpider.Tool.Http;

public static class HttpClientTools
{
    // 开启压缩响应自动解压 ( gzip/deflate/br ) , Accept-Encoding 请求头也由 handler 自动携带。
    // 注意 : 不能手动 Add("Accept-Encoding", ...) —— 手动设置后 SocketsHttpHandler 会视为调用方自行处理压缩 , 自动解压随之失效。
    // DecompressionMethods 暂不含 zstd , 请求头也不声明 zstd ( 避免"声明了却解不开"的乱码雷 )。
    private static readonly HttpClientHandler DecompressHandler = new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.All
    };

    private static readonly HttpClient HttpClient = new HttpClient(DecompressHandler);
    private static readonly Object Lock = new object();
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
            httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
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
}