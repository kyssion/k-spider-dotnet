namespace KSpider.Spider.Verify.Detector;

/// <summary>
///     验证码识别器 : 滑块 / 短信 / 图形三类。
///     特征分两档 —— 厂商特有的 DOM 标识与脚本名单独即可判定 ; 中文提示语必须配合表单元素
///     ( <c>&lt;form</c> / <c>&lt;input</c> ) 才算数 , 否则一篇讲验证码的新闻正文也会被误判成验证页。
///     图形与短信验证码需要识图或人工确认 , 本模块只负责识别与升级告警 ( 见 ManualEscalationSolver ) 。
/// </summary>
public sealed class CaptchaDetector : IVerificationDetector
{
    /// <summary>滑块 : 阿里云盾 / 极验 / 通用自研滑块的 DOM 标识与脚本名</summary>
    private static readonly string[] SliderStrongMarkers =
    [
        "nc_1_n1z", "nc-container", "nc_wrapper", "nc_scale",
        "aliyunCaptcha",
        "geetest_slider_button", "geetest_panel", "gt_slider",
        "slide-verify", "sliderCaptcha", "btn_slide", "verify-slider"
    ];

    private static readonly string[] SliderProseMarkers =
    [
        "拖动滑块", "滑动验证", "按住滑块", "向右滑动填充拼图"
    ];

    /// <summary>短信验证码 : 必须排在图形验证码之前判 , 短信页同样含"验证码"字样</summary>
    private static readonly string[] SmsStrongMarkers = ["sendSmsCode", "smsCode"];

    private static readonly string[] SmsProseMarkers = ["发送验证码", "短信验证码", "手机验证码", "获取验证码"];

    /// <summary>图形验证码 : 具体的元素标识</summary>
    private static readonly string[] ImageStrongMarkers =
    [
        "captcha_img", "captcha-image", "captchaImg", "clickCaptcha",
        "verifyCode", "verify_code", "checkCode", "imgCode",
        "id=\"captcha\"", "name=\"captcha\"", "class=\"captcha\""
    ];

    private static readonly string[] ImageProseMarkers = ["请输入验证码", "图形验证码", "看不清，换一张"];

    public string Name => "CaptchaDetector";

    public VerificationChallenge? Detect(VerificationProbe probe)
    {
        if (!probe.IsHtml) return null;
        var body = probe.BodySample;
        var hasForm = body.Contains("<form", StringComparison.OrdinalIgnoreCase)
                      || body.Contains("<input", StringComparison.OrdinalIgnoreCase);

        var slider = Match(body, SliderStrongMarkers) ?? MatchProse(body, SliderProseMarkers, hasForm);
        if (slider != null) return Build(VerificationKind.SliderCaptcha, slider);

        var sms = Match(body, SmsStrongMarkers) ?? MatchProse(body, SmsProseMarkers, hasForm);
        if (sms != null) return Build(VerificationKind.SmsCaptcha, sms);

        var image = Match(body, ImageStrongMarkers) ?? MatchProse(body, ImageProseMarkers, hasForm);
        return image == null ? null : Build(VerificationKind.ImageCaptcha, image);
    }

    private VerificationChallenge Build(VerificationKind kind, string evidence)
    {
        return new VerificationChallenge
        {
            Kind = kind,
            DetectorName = Name,
            Evidence = evidence
        };
    }

    private static string? Match(string body, string[] markers)
    {
        foreach (var marker in markers)
            if (body.Contains(marker, StringComparison.OrdinalIgnoreCase))
                return $"命中特征 \"{marker}\"";
        return null;
    }

    private static string? MatchProse(string body, string[] markers, bool hasForm)
    {
        if (!hasForm) return null;
        var hit = Match(body, markers);
        return hit == null ? null : $"{hit} ( 页面含表单元素 )";
    }
}
