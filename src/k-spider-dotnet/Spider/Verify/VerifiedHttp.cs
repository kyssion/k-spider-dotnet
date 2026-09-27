using KSpider.Exceptions;
using KSpider.Spider.Verify.Model;
using KSpider.Common.Http;
using KSpider.Common;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.Verify;

/// <summary>
///     抓取层门面 : 带"自动识别并过验证"的 HTTP 请求。爬虫不再直接调
///     <see cref="HttpClientTools" /> —— 走这里才能在被拦住时自动过验证并重放请求。
///     正常响应只多跑一遍纯字符串判定 ( 无网络、无浏览器开销 ) , 代价可以忽略。
/// </summary>
public static class VerifiedHttp
{
    // 静态类不能作 LogFactory.GetLogger 的类型参数 ( 见 AGENTS ) , 传 typeof
    private static readonly ILogger Log = LogFactory.GetLogger(typeof(VerifiedHttp));

    /// <summary>GET 请求 , 自动带当前会话 cookie 并在被拦截时过验证重放</summary>
    public static Task<string> GetStringAsync(string host, string url, CancellationToken cancellationToken = default)
    {
        return SendStringAsync(host, () => new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
    }

    /// <summary>
    ///     需要自定义请求头 ( 如金十的 x-app-id ) 时用这个重载 : 传的是请求工厂而不是请求实例 ——
    ///     HttpRequestMessage 不能重复发送 , 过完验证重放时必须造一个新的。
    /// </summary>
    public static async Task<string> SendStringAsync(string host, Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken = default)
    {
        var policy = VerificationRegistry.PolicyFor(host);
        // 最多发几次请求 ( 含过验证后的重放 ) : 每次被拦都会走一遍过验证 , 默认 2 次即"解一次 + 重放一次"
        var maxAttempts = Math.Max(1, policy.MaxSolveAttempts);
        for (var attempt = 1;; attempt++)
        {
            var session = VerificationRegistry.SessionStore.Get(host);
            using var request = requestFactory();
            var url = request.RequestUri?.ToString() ?? "";
            // 会话回放走 CookieContainer : 直接给请求加 Cookie 头会与容器里的同名旧值拼成两份 ( 见 HttpClientTools.ApplyCookies ) ;
            // 其余请求头 ( UA / sec-ch-ua 等 ) 由 HttpClientTools 统一伪装 , 与浏览器侧保持一致
            if (session != null) HttpClientTools.ApplyCookies(url, session.Cookies);

            using var response = await HttpClientTools.CreateByHost(host).SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var probe = VerificationProbe.FromResponse(url, response, body);
            var challenge = VerificationRegistry.Pipeline.Detect(probe);
            if (challenge == null)
            {
                // 没被拦住 : 状态码异常按原有语义抛 ( 与直接调 HttpClient.GetStringAsync 一致 )
                if (!response.IsSuccessStatusCode)
                    throw new DownloadHttpRequestException(url,
                        $"[VerifiedHttp] 接口返回状态码 {(int)response.StatusCode}");
                return body;
            }

            var outcome = await VerificationRegistry.Pipeline.HandleAsync(probe, cancellationToken);
            if (!outcome.Solved)
                throw new VerificationRequiredException(url, challenge.Kind,
                    $"[VerifiedHttp] host {host} 被 {challenge.Kind} 拦住且未通过 : {outcome.Message} ; 依据 : {challenge.Evidence}");

            if (attempt >= maxAttempts)
                throw new VerificationRequiredException(url, challenge.Kind,
                    $"[VerifiedHttp] host {host} 已过验证但重放 {attempt} 次仍被 {challenge.Kind} 拦住 , 放弃本条请求");

            Log.LogWarning(
                "[VerifiedHttp] 已过验证 , 重放请求 ( 第 {Attempt} 次 ) , host : {Host} , url : {Url} , {Message}",
                attempt, host, url, outcome.Message);
        }
    }
}
