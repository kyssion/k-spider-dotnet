using System.Globalization;
using System.Net.Http;

namespace KSpider.Spider.Verify;

/// <summary>
///     验证 ( 反爬 ) 类型 : 识别器与通过策略共用的统一词汇表。
///     两侧只依赖这个枚举 , 互不引用具体实现 —— 新增一种验证方式时在这里加值 ,
///     由识别器返回它、由通过策略声明自己能处理它即可。
/// </summary>
public enum VerificationKind
{
    /// <summary>拒绝访问 ( 403 / 401 ) , 常见于 WAF 直接拦截或需要带 cookie</summary>
    AccessDenied = 1,

    /// <summary>频率限制 ( 429 ) , 只能退避 , 不存在"通过"一说</summary>
    RateLimited = 2,

    /// <summary>网关拦截 ( 503 且带 Retry-After ) , 多为 WAF 限流而非服务真的挂了</summary>
    ServerGate = 3,

    /// <summary>3xx 跳转到验证 / 风控页</summary>
    VerifyRedirect = 4,

    /// <summary>Cloudflare 类 JS 挑战 ( cf-mitigated / challenge-platform / Turnstile )</summary>
    CloudflareChallenge = 10,

    /// <summary>JS 计算 cookie 型门禁 ( 加速乐 __jsl_clearance / 阿里云盾 acw_sc__v2 / 瑞数 ) , 浏览器执行脚本后自动通过</summary>
    JsCookieGate = 11,

    /// <summary>通用"正在验证浏览器 / 请开启 JavaScript"页</summary>
    BrowserCheck = 12,

    /// <summary>滑块验证码</summary>
    SliderCaptcha = 20,

    /// <summary>图形验证码 ( 需识图 , 本模块只识别与升级 )</summary>
    ImageCaptcha = 21,

    /// <summary>短信 / 手机验证码</summary>
    SmsCaptcha = 22,

    /// <summary>HTTP 200 但载荷里报风控 / 鉴权失败 ( 静默失效 )</summary>
    RiskControl = 30
}

/// <summary>
///     验证类型的共性判断 , 供管线与通过策略共用。
///     放在枚举旁边 : 新增枚举值时就该顺手想清楚它属不属于这两类。
/// </summary>
public static class VerificationKindTraits
{
    /// <summary>
    ///     只能靠"等"恢复的类型 ( 限流 / 载荷级风控 ) : 没有任何自动手段 ,
    ///     也不该升级成"需要人工介入" —— 退避一段时间自己就好了。
    /// </summary>
    public static bool ResolvesByWaiting(VerificationKind kind)
    {
        return kind is VerificationKind.RateLimited or VerificationKind.RiskControl;
    }

    /// <summary>
    ///     与 cookie 有关的类型 : 被这类拦住说明手上的会话已不被接受 , 该丢掉 ,
    ///     否则冷却期内每个请求都继续拿着死 cookie 去撞。
    ///     限流 / 风控与 cookie 无关 , 丢掉有效会话反而让下一个请求白多过一次验证。
    /// </summary>
    public static bool IsCookieRelated(VerificationKind kind)
    {
        return kind is VerificationKind.AccessDenied or VerificationKind.VerifyRedirect
            or VerificationKind.CloudflareChallenge or VerificationKind.JsCookieGate
            or VerificationKind.BrowserCheck;
    }
}

/// <summary>
///     一次识别结果 : 命中了哪种验证、由谁命中、证据是什么。
///     Evidence 进日志 , 排障时据此判断是源站改版还是风控策略变化。
/// </summary>
public sealed class VerificationChallenge
{
    public required VerificationKind Kind { get; init; }

    /// <summary>命中的识别器名 , 用于日志定位</summary>
    public required string DetectorName { get; init; }

    /// <summary>判定依据 ( 命中的特征片段 ) , 只作日志与排障 , 不参与逻辑</summary>
    public string Evidence { get; init; } = "";

    public int StatusCode { get; init; }

    /// <summary>3xx 跳转目标</summary>
    public string? Location { get; init; }

    /// <summary>429 / 503 响应头里的 Retry-After ( 秒 ) , 0 表示没给</summary>
    public int RetryAfterSeconds { get; init; }
}

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

    public string Url { get; }

    /// <summary>从 Url 解析出的主机名 , 会话缓存与源级策略都按它索引</summary>
    public string Host { get; }

    public int StatusCode { get; }

    /// <summary>响应体取样 ( 最长 MaxBodySampleLength )</summary>
    public string BodySample { get; }

    /// <summary>响应体完整长度 ( BodySample 被截断时用于区分"短挑战页"与"长正文页" )</summary>
    public int BodyLength { get; }

    public IReadOnlyDictionary<string, string> Headers { get; }

    public string? ContentType { get; }

    public string? Location { get; }

    public int RetryAfterSeconds { get; }

    public bool IsHtml { get; }

    public string? Header(string name)
    {
        return Headers.TryGetValue(name, out var value) ? value : null;
    }

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

/// <summary>
///     通过策略的输入
/// </summary>
public sealed class VerificationSolveRequest
{
    public required VerificationChallenge Challenge { get; init; }
    public required VerificationProbe Probe { get; init; }
    public required VerificationPolicy Policy { get; init; }

    public string Host => Probe.Host;
    public string Url => Probe.Url;
}

/// <summary>
///     通过策略的输出 : 成功时带回可用于重放请求的会话
/// </summary>
public sealed class VerificationSolveResult
{
    public required string SolverName { get; init; }
    public bool Success { get; init; }
    public VerificationSession? Session { get; init; }
    public string Message { get; init; } = "";

    /// <summary>是否需要人工介入 ( 自动手段都用尽 ) , 由 NewsCheckJob 汇总告警</summary>
    public bool NeedsManualAttention { get; init; }

    public static VerificationSolveResult Solved(string solverName, VerificationSession session, string message)
    {
        return new VerificationSolveResult { SolverName = solverName, Success = true, Session = session, Message = message };
    }

    public static VerificationSolveResult Failed(string solverName, string message, bool needsManualAttention = false)
    {
        return new VerificationSolveResult
        {
            SolverName = solverName,
            Success = false,
            Message = message,
            NeedsManualAttention = needsManualAttention
        };
    }
}

/// <summary>
///     一次"识别 + 通过"的最终结论
/// </summary>
public sealed class VerificationOutcome
{
    public bool Challenged { get; init; }
    public VerificationChallenge? Challenge { get; init; }
    public bool Solved { get; init; }
    public VerificationSession? Session { get; init; }
    public string Message { get; init; } = "";
    public bool NeedsManualAttention { get; init; }

    /// <summary>响应正常 , 没有识别到任何验证</summary>
    public static VerificationOutcome NotChallenged { get; } = new() { Challenged = false };

    public static VerificationOutcome Passed(VerificationChallenge challenge, VerificationSession session, string message)
    {
        return new VerificationOutcome
        {
            Challenged = true, Challenge = challenge, Solved = true, Session = session, Message = message
        };
    }

    public static VerificationOutcome Blocked(VerificationChallenge challenge, string message,
        bool needsManualAttention)
    {
        return new VerificationOutcome
        {
            Challenged = true,
            Challenge = challenge,
            Solved = false,
            Message = message,
            NeedsManualAttention = needsManualAttention
        };
    }
}

/// <summary>
///     当前被验证挡住的源 ( 处于冷却期 ) , 供健康检查汇总告警
/// </summary>
public sealed class VerificationBlock
{
    public required string Host { get; init; }
    public required VerificationKind Kind { get; init; }
    public required DateTime Until { get; init; }
    public required string Reason { get; init; }
}
