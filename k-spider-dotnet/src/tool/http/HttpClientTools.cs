namespace k_spider_dotnet.tool.http;

public static class HttpClientTools
{
    private static readonly HttpClient HttpClient = new HttpClient();
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
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip,deflate,br,zstd");
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