namespace KSpider.Spider.Verify.Detector;

/// <summary>
///     传输层门禁识别器 : 只看状态码与跳转目标 , 不看响应体。
///     覆盖 WAF 直接拒绝 ( 403 ) 、限流 ( 429 ) 、网关拦截 ( 503 + Retry-After ) 与"被赶去验证页" ( 3xx )。
/// </summary>
public sealed class HttpGateDetector : IVerificationDetector
{
    /// <summary>
    ///     跳转目标里出现这些片段才认为是被赶去验证页 —— 普通 3xx ( 站点改版跳转 / 补斜杠 ) 不算验证 ,
    ///     误判的代价是白白启动一次浏览器并让这条数据失败
    /// </summary>
    private static readonly string[] VerifyLocationKeywords =
    [
        "/verify", "/captcha", "/challenge", "/security", "/antibot", "/risk", "/seccheck",
        "waf", "jsl", "check.html", "blocked"
    ];

    public string Name => "HttpGateDetector";

    public VerificationChallenge? Detect(VerificationProbe probe)
    {
        if (probe.StatusCode is >= 300 and < 400)
        {
            var location = probe.Location;
            if (string.IsNullOrEmpty(location)) return null;
            var keyword = VerifyLocationKeywords.FirstOrDefault(
                item => location.Contains(item, StringComparison.OrdinalIgnoreCase));
            if (keyword == null) return null;
            return new VerificationChallenge
            {
                Kind = VerificationKind.VerifyRedirect,
                DetectorName = Name,
                StatusCode = probe.StatusCode,
                Location = location,
                Evidence = $"Location 命中 \"{keyword}\" : {location}"
            };
        }

        switch (probe.StatusCode)
        {
            case 401:
            case 403:
                return new VerificationChallenge
                {
                    Kind = VerificationKind.AccessDenied,
                    DetectorName = Name,
                    StatusCode = probe.StatusCode,
                    Evidence = $"HTTP {probe.StatusCode}"
                };
            case 412:
                // 412 是不少 WAF 对"没带 JS 门禁 cookie 的客户端"的标准回法 ( 瑞数等 ) , 误判面很小
                return new VerificationChallenge
                {
                    Kind = VerificationKind.AccessDenied,
                    DetectorName = Name,
                    StatusCode = probe.StatusCode,
                    Evidence = "HTTP 412 ( 常见于 WAF 要求先过 JS 门禁 )"
                };
            case 429:
                return new VerificationChallenge
                {
                    Kind = VerificationKind.RateLimited,
                    DetectorName = Name,
                    StatusCode = probe.StatusCode,
                    RetryAfterSeconds = probe.RetryAfterSeconds,
                    Evidence = probe.RetryAfterSeconds > 0 ? $"HTTP 429 , Retry-After={probe.RetryAfterSeconds}s" : "HTTP 429"
                };
            case 503 when probe.RetryAfterSeconds > 0:
                // 503 也可能是服务真的挂了 : 只有带 Retry-After ( WAF 限流的典型特征 ) 才当作拦截类 ,
                // 否则交给普通状态码异常 , 不要给运维一个假的"被反爬了"
                return new VerificationChallenge
                {
                    Kind = VerificationKind.ServerGate,
                    DetectorName = Name,
                    StatusCode = probe.StatusCode,
                    RetryAfterSeconds = probe.RetryAfterSeconds,
                    Evidence = $"HTTP 503 , Retry-After={probe.RetryAfterSeconds}s"
                };
            default:
                return null;
        }
    }
}
