namespace KSpider.Spider.Verify.Detector;

/// <summary>
///     Cloudflare 类 JS 挑战识别器 : 这类挑战由页面脚本自动完成 ,
///     浏览器打开一次就能拿到放行 cookie ( 见 BrowserChallengeSolver ) 。
/// </summary>
public sealed class CloudflareDetector : IVerificationDetector
{
    /// <summary>
    ///     页面特征。刻意不把 <c>Server: cloudflare</c> 单独当证据 —— 正常响应也带这个头 ,
    ///     必须配合挑战页特征才算命中。
    /// </summary>
    private static readonly string[] BodyMarkers =
    [
        "Just a moment",
        "cf-browser-verification",
        "cf_chl_",
        "_cf_chl_opt",
        "cdn-cgi/challenge-platform",
        "Checking your browser before accessing",
        "Verifying you are human",
        "Attention Required!",
        "cf-please-wait",
        "cf-turnstile",
        "challenges.cloudflare.com"
    ];

    public string Name => "CloudflareDetector";

    public VerificationChallenge? Detect(VerificationProbe probe)
    {
        // 响应头是最硬的证据 : cf-mitigated 是 Cloudflare 自己标注"这次响应属于拦截"
        var mitigated = probe.Header("cf-mitigated");
        if (!string.IsNullOrEmpty(mitigated))
            return new VerificationChallenge
            {
                Kind = VerificationKind.CloudflareChallenge,
                DetectorName = Name,
                StatusCode = probe.StatusCode,
                Evidence = $"cf-mitigated: {mitigated}"
            };

        if (!probe.IsHtml) return null;
        var marker = BodyMarkers.FirstOrDefault(
            item => probe.BodySample.Contains(item, StringComparison.OrdinalIgnoreCase));
        if (marker == null) return null;
        return new VerificationChallenge
        {
            Kind = VerificationKind.CloudflareChallenge,
            DetectorName = Name,
            StatusCode = probe.StatusCode,
            Evidence = $"挑战页命中 \"{marker}\" , Server={probe.Header("Server")}"
        };
    }
}
