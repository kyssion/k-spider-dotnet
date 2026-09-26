using System.Globalization;
using System.Net.Http;

namespace KSpider.Spider.Verify.Model;

/// <summary>
///     识别器与通过策略的输入 : 一次响应的可判定信息 ( 状态码 + 头 + 响应体取样 )。
///     刻意做成不依赖 HttpResponseMessage 的纯数据 , 识别器因此可以离线用夹具回归。
/// </summary>
public sealed class VerificationProbe
{
    /// <summary>
    ///     响应体取样上限 : 挑战特征都出现在文档头部 , 截断后既够判定 , 又避免对整篇正文跑正则
    /// </summary>
    public const int MaxBodySampleLength = 64 * 1024;

    private static readonly IReadOnlyDictionary<string, string> EmptyHeaders =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public VerificationProbe(string url, int statusCode, string body,
        IReadOnlyDictionary<string, string>? headers = null)
    {
        Url = url;
        Host = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : "";
        StatusCode = statusCode;
        BodyLength = body.Length;
        BodySample = body.Length > MaxBodySampleLength ? body[..MaxBodySampleLength] : body;
        Headers = headers ?? EmptyHeaders;
        ContentType = Header("Content-Type");
        Location = Header("Location");
        RetryAfterSeconds = ParseRetryAfter(Header("Retry-After"));
        // 内容型识别器只对 HTML 生效 : 各源正常业务载荷都是 JSON ,
        // 若对 JSON 也跑"页面特征"匹配 , 一篇提到验证码的新闻就会误判成被拦截
        IsHtml = ContentType != null
            ? ContentType.Contains("html", StringComparison.OrdinalIgnoreCase)
            : BodySample.TrimStart().StartsWith('<');
    }

    /// <summary>原始请求地址 ( Host 即由它解析 )</summary>
    public string Url { get; }

    /// <summary>从 Url 解析出的主机名 , 会话缓存与源级策略都按它索引</summary>
    public string Host { get; }

    /// <summary>响应状态码</summary>
    public int StatusCode { get; }

    /// <summary>响应体取样 ( 最长 MaxBodySampleLength )</summary>
    public string BodySample { get; }

    /// <summary>响应体完整长度 ( BodySample 被截断时用于区分"短挑战页"与"长正文页" )</summary>
    public int BodyLength { get; }

    /// <summary>响应头快照 ( 名不分大小写 , 多值以逗号拼接 )</summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>Content-Type 头 , 判 IsHtml 用</summary>
    public string? ContentType { get; }

    /// <summary>Location 头 ( 3xx 跳转目标 )</summary>
    public string? Location { get; }

    /// <summary>Retry-After 头折算成秒 ( 0 = 未提供 )</summary>
    public int RetryAfterSeconds { get; }

    /// <summary>响应是否为 HTML : 内容型识别器只对 HTML 生效 , 各源正常业务载荷都是 JSON</summary>
    public bool IsHtml { get; }

    /// <summary>按名取响应头 , 不存在返回 null</summary>
    public string? Header(string name)
    {
        return Headers.TryGetValue(name, out var value) ? value : null;
    }

    /// <summary>从 HttpResponseMessage 组装探针 ( 抓取层在拿到响应后调用 )</summary>
    public static VerificationProbe FromResponse(string url, HttpResponseMessage response, string body)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in response.Headers) headers[header.Key] = string.Join(",", header.Value);
        foreach (var header in response.Content.Headers) headers[header.Key] = string.Join(",", header.Value);
        return new VerificationProbe(url, (int)response.StatusCode, body, headers);
    }

    /// <summary>
    ///     Retry-After 有两种合法写法 ( 秒数 或 HTTP 日期 ) , 两种都要认 :
    ///     认不出会让 503 漏判成"服务挂了"、429 的退避窗口失效。
    /// </summary>
    private static int ParseRetryAfter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        if (int.TryParse(value, out var seconds)) return Math.Max(0, seconds);
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal,
            out var date)
            ? Math.Max(0, (int)(date - DateTimeOffset.UtcNow).TotalSeconds)
            : 0;
    }
}
