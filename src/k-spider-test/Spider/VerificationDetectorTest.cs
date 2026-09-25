using KSpider.Spider.Verify;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     反爬验证识别回归 ( 全部离线 ) :
///     1. 各形态挑战页能识别出正确类型 ( 夹具为按公开特征构造的样本 , 来源见 TestData/README.md )
///     2. 五个源的真实业务响应一条都不许误判 —— 误判的代价是白起一次浏览器并让这条数据失败
/// </summary>
[TestClass]
public class VerificationDetectorTest
{
    private const string HtmlContentType = "text/html; charset=utf-8";
    private const string JsonContentType = "application/json";
    private const string ProbeUrl = "https://www.example.com/news/list";

    private static string LoadFixture(string fileName)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
    }

    private static Dictionary<string, string> Headers(string contentType,
        params (string Name, string Value)[] extraHeaders)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Content-Type"] = contentType
        };
        foreach (var (name, value) in extraHeaders) headers[name] = value;
        return headers;
    }

    private static VerificationProbe HtmlProbe(string fileName, int statusCode = 200)
    {
        return new VerificationProbe(ProbeUrl, statusCode, LoadFixture(fileName), Headers(HtmlContentType));
    }

    /// <summary>业务响应夹具探针 : .json 按 application/json 、.html 按 text/html , 与线上一致</summary>
    private static VerificationProbe FixtureProbe(string fileName)
    {
        var contentType = fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? JsonContentType
            : HtmlContentType;
        return new VerificationProbe(ProbeUrl, 200, LoadFixture(fileName), Headers(contentType));
    }

    private static VerificationKind? KindOf(VerificationProbe probe)
    {
        return VerificationRegistry.Pipeline.Detect(probe)?.Kind;
    }

    [TestMethod]
    public void DetectCloudflareChallengePage()
    {
        Assert.AreEqual(VerificationKind.CloudflareChallenge, KindOf(HtmlProbe("verify_cloudflare_challenge.html")));
    }

    [TestMethod]
    public void DetectCloudflareByMitigatedHeader()
    {
        // 响应头是比页面特征更硬的证据 : 即便响应体不是挑战页也能判
        var probe = new VerificationProbe(ProbeUrl, 403, "blocked",
            Headers(JsonContentType, ("cf-mitigated", "challenge")));
        Assert.AreEqual(VerificationKind.CloudflareChallenge, KindOf(probe));
    }

    [TestMethod]
    [DataRow("verify_jsl_clearance.html")]
    [DataRow("verify_acw_sc_v2.html")]
    public void DetectJsCookieGate(string fileName)
    {
        Assert.AreEqual(VerificationKind.JsCookieGate, KindOf(HtmlProbe(fileName)));
    }

    [TestMethod]
    [DataRow("verify_slider_aliyun.html")]
    [DataRow("verify_aliyun_waf_captcha.html")]
    [DataRow("verify_slider_geetest.html")]
    public void DetectSliderCaptcha(string fileName)
    {
        Assert.AreEqual(VerificationKind.SliderCaptcha, KindOf(HtmlProbe(fileName)));
    }

    [TestMethod]
    public void DetectImageCaptcha()
    {
        Assert.AreEqual(VerificationKind.ImageCaptcha, KindOf(HtmlProbe("verify_image_captcha.html")));
    }

    [TestMethod]
    public void DetectSmsCaptcha()
    {
        // 短信页同样含"验证码"字样 , 必须先于图形验证码判定
        Assert.AreEqual(VerificationKind.SmsCaptcha, KindOf(HtmlProbe("verify_sms_captcha.html")));
    }

    [TestMethod]
    [DataRow(401, VerificationKind.AccessDenied)]
    [DataRow(403, VerificationKind.AccessDenied)]
    [DataRow(412, VerificationKind.AccessDenied)]
    [DataRow(429, VerificationKind.RateLimited)]
    public void DetectTransportGateByStatus(int statusCode, VerificationKind expected)
    {
        var probe = new VerificationProbe(ProbeUrl, statusCode, "", Headers(HtmlContentType));
        Assert.AreEqual(expected, KindOf(probe));
    }

    [TestMethod]
    public void RetryAfterHttpDateIsParsed()
    {
        // Retry-After 允许写成 HTTP 日期 : 认不出会让 503 漏判成"服务挂了"、429 的退避窗口失效
        var retryAt = DateTimeOffset.UtcNow.AddSeconds(120).ToString("R");
        var probe = new VerificationProbe(ProbeUrl, 503, "", Headers(HtmlContentType, ("Retry-After", retryAt)));

        var challenge = VerificationRegistry.Pipeline.Detect(probe);

        Assert.AreEqual(VerificationKind.ServerGate, challenge?.Kind);
        Assert.IsTrue(challenge!.RetryAfterSeconds is > 90 and <= 120, $"退避秒数解析错 : {challenge.RetryAfterSeconds}");
    }

    [TestMethod]
    public void DetectServerGateOnlyWithRetryAfter()
    {
        // 503 也可能是服务真的挂了 : 只有带 Retry-After ( WAF 限流的典型特征 ) 才判拦截 ,
        // 否则交给普通状态码异常 , 不给运维一个假的"被反爬了"
        var withRetryAfter = new VerificationProbe(ProbeUrl, 503, "",
            Headers(HtmlContentType, ("Retry-After", "120")));
        var challenge = VerificationRegistry.Pipeline.Detect(withRetryAfter);
        Assert.AreEqual(VerificationKind.ServerGate, challenge?.Kind);
        Assert.AreEqual(120, challenge?.RetryAfterSeconds);

        var plain = new VerificationProbe(ProbeUrl, 503, "", Headers(HtmlContentType));
        Assert.IsNull(KindOf(plain));
    }

    [TestMethod]
    public void DetectVerifyRedirect()
    {
        var probe = new VerificationProbe(ProbeUrl, 302, "",
            Headers(HtmlContentType, ("Location", "https://www.example.com/verify?redirect=%2Fnews")));
        Assert.AreEqual(VerificationKind.VerifyRedirect, KindOf(probe));
    }

    [TestMethod]
    public void PlainRedirectIsNotChallenge()
    {
        // 普通跳转 ( 补斜杠 / 站点改版 ) 不能判成验证
        var probe = new VerificationProbe(ProbeUrl, 301, "",
            Headers(HtmlContentType, ("Location", "https://www.example.com/news/list/")));
        Assert.IsNull(KindOf(probe));
    }

    [TestMethod]
    public void DetectRiskControlInJsonPayload()
    {
        // 状态码 200 、载荷报风控 : 最危险的一类拦截 ( 看着请求成功 , 实际一条数据都拿不到 )
        const string body = """{"code":403,"message":"访问过于频繁，请稍后再试","data":null}""";
        var probe = new VerificationProbe(ProbeUrl, 200, body, Headers(JsonContentType));
        Assert.AreEqual(VerificationKind.RiskControl, KindOf(probe));
    }

    [TestMethod]
    public void SignatureErrorIsNotRiskControl()
    {
        // 财联社签名错误是前端版本号过期 ( 属源改版 , 有独立处理路径 ) , 归到风控会把运维引向错误方向
        const string body = """{"errno":"10012","msg":"签名错误"}""";
        Assert.IsNull(KindOf(new VerificationProbe("https://www.cls.cn/detail/1", 200, body, Headers(JsonContentType))));
    }

    [TestMethod]
    public void EnvelopeRiskMessageIsRiskControl()
    {
        // 提示语挂在响应 envelope 上 ( 对象层级 ) 才是风控
        const string body = """{"code":403,"data":{"message":"访问过于频繁，请稍后再试"}}""";
        Assert.AreEqual(VerificationKind.RiskControl,
            KindOf(new VerificationProbe(ProbeUrl, 200, body, Headers(JsonContentType))));
    }

    [TestMethod]
    public void BusinessItemFieldsAreNotRiskControl()
    {
        // 业务数据都在数组里 : 某条新闻的 desc 含英文 risk 不能把整个源判成风控 ( 否则一源进冷却 )
        const string body =
            """{"code":20000,"message":"OK","data":{"items":[{"id":1,"desc":"market risk rises amid volatility"}]}}""";
        Assert.IsNull(KindOf(new VerificationProbe(ProbeUrl, 200, body, Headers(JsonContentType))));
    }

    [TestMethod]
    public void SourceSideValidationMessageIsNotRiskControl()
    {
        // "校验 / 请求异常"是源站自己的参数或内部报错 , 不是风控
        const string body = """{"errno":"10001","msg":"参数校验失败：last_time 格式错误"}""";
        Assert.IsNull(KindOf(new VerificationProbe(ProbeUrl, 200, body, Headers(JsonContentType))));
    }

    [TestMethod]
    public void CaptchaWordInArticleBodyIsNotChallenge()
    {
        // 中文提示语必须配合表单元素才算数 : 讲验证码的新闻正文也会出现"图形验证码"三个字
        const string body =
            "<html><body><article><p>多家平台上线了图形验证码，用户需要输入验证码才能继续访问。</p></article></body></html>";
        Assert.IsNull(KindOf(new VerificationProbe("https://www.example.com/news/1", 200, body, Headers(HtmlContentType))));
    }

    [TestMethod]
    public void DetectCaptchaByProseMarkerWithForm()
    {
        // 只有中文提示语、没有厂商 DOM 标识的验证页 , 靠"提示语 + 表单"兜住
        const string body =
            "<html><body><form action='/v' method='post'><p>请输入图形验证码</p><input name='code'></form></body></html>";
        Assert.AreEqual(VerificationKind.ImageCaptcha,
            KindOf(new VerificationProbe(ProbeUrl, 200, body, Headers(HtmlContentType))));
    }

    [TestMethod]
    public void LongHtmlPageWithCookieScriptIsNotJsGate()
    {
        // 通用兜底规则只对薄壳页生效 : 长正文页里的 document.cookie + 跳转不该判成 JS 门禁
        var body = "<html><body><script>document.cookie='a=1';location.href='/next';</script>"
                   + new string('x', 20000) + "</body></html>";
        Assert.IsNull(KindOf(new VerificationProbe("https://www.example.com/news/1", 200, body, Headers(HtmlContentType))));
    }

    [TestMethod]
    [DataRow("cls_roll_page1.json")]
    [DataRow("cls_roll_page2.json")]
    [DataRow("cls_roll_with_image.json")]
    [DataRow("cls_depth_list_1000_page1.json")]
    [DataRow("cls_depth_list_1000_page2.json")]
    [DataRow("df_list_344.json")]
    [DataRow("df_article_real.json")]
    [DataRow("sina_live_page1.json")]
    [DataRow("sina_live_page2.json")]
    [DataRow("wscn_live_page1.json")]
    [DataRow("wscn_live_page2.json")]
    [DataRow("jin10_flash_page1.json")]
    [DataRow("jin10_flash_page2.json")]
    [DataRow("cls_article_detail.html")]
    [DataRow("cls_article_detail_rich.html")]
    public void RealBusinessPayloadIsNotChallenge(string fileName)
    {
        Assert.IsNull(KindOf(FixtureProbe(fileName)), $"真实业务响应 {fileName} 被误判为验证");
    }
}
