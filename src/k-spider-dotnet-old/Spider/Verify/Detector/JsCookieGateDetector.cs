using KSpider.Spider.Verify.Model;

namespace KSpider.Spider.Verify.Detector;

/// <summary>
///     JS 计算 cookie 型门禁识别器 : 站点返回一个薄壳页 , 页内脚本算出校验值写进 cookie 再跳回原地址。
///     加速乐 ( __jsl_clearance ) 、阿里云盾 ( acw_sc__v2 ) 、瑞数、长亭雷池都属这一类 ,
///     共同点是"浏览器执行一次脚本就自动通过" , 因此由浏览器策略统一处理。
/// </summary>
public sealed class JsCookieGateDetector : IVerificationDetector
{
    /// <summary>各厂商脚本特征 , 命中任意一个即可判定</summary>
    private static readonly (string Marker, string Owner)[] VendorMarkers =
    [
        ("__jsl_clearance", "加速乐"),
        ("__jsluid", "加速乐"),
        ("acw_sc__v2", "阿里云盾"),
        ("acw_tc", "阿里云盾"),
        ("$_ts", "瑞数"),
        ("_$sdk", "瑞数"),
        ("yunsuo_session", "云锁"),
        ("safeline", "长亭雷池 WAF"),
        ("__waf", "通用 WAF")
    ];

    /// <summary>
    ///     通用兜底判定的响应体长度上限 : JS 门禁页都是薄壳页 ( 通常几百字节 ) ,
    ///     长正文页即便同时出现 document.cookie 与跳转也不该判成门禁
    /// </summary>
    private const int GenericGateMaxBodyLength = 8192;

    /// <summary>识别器名 , 会写进 <see cref="VerificationChallenge.DetectorName" /></summary>
    public string Name => "JsCookieGateDetector";

    /// <summary>先按厂商脚本特征精确判定 ; 未命中再用"薄壳页写 cookie + 跳转"兜底</summary>
    public VerificationChallenge? Detect(VerificationProbe probe)
    {
        if (!probe.IsHtml) return null;

        foreach (var (marker, owner) in VendorMarkers)
            if (probe.BodySample.Contains(marker, StringComparison.OrdinalIgnoreCase))
                return new VerificationChallenge
                {
                    Kind = VerificationKind.JsCookieGate,
                    DetectorName = Name,
                    StatusCode = probe.StatusCode,
                    Evidence = $"命中 {owner} 特征 \"{marker}\""
                };

        // 通用兜底 : "薄壳页里写 cookie 再跳转"是这一类门禁的共性 , 没命中厂商特征时靠它兜住
        if (probe.BodyLength > GenericGateMaxBodyLength) return null;
        var body = probe.BodySample;
        if (!body.Contains("document.cookie", StringComparison.OrdinalIgnoreCase)) return null;
        // 四种常见跳转写法命中任意一种即可
        var redirects = body.Contains("location.href", StringComparison.OrdinalIgnoreCase)
                        || body.Contains("location.replace", StringComparison.OrdinalIgnoreCase)
                        || body.Contains("location.reload", StringComparison.OrdinalIgnoreCase)
                        || body.Contains("window.location", StringComparison.OrdinalIgnoreCase);
        if (!redirects) return null;
        return new VerificationChallenge
        {
            Kind = VerificationKind.JsCookieGate,
            DetectorName = Name,
            StatusCode = probe.StatusCode,
            Evidence = $"薄壳页 ({probe.BodyLength} 字节) 内 document.cookie + 跳转"
        };
    }
}
