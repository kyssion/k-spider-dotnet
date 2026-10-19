namespace KSpider.Spider.Verify.Model;

/// <summary>
///     一次"已通过验证"的会话 : 过验证拿到的 cookie , 回放给 HTTP 链路复用。
///     只回放 cookie 不回放 User-Agent —— 请求头由 <see cref="Tool.Http.HttpClientTools" /> 统一伪装 ,
///     浏览器侧也按同一串 UA 打开 ( 见 BrowserChallengeSolver ) , 两边指纹保持一致。
/// </summary>
public sealed class VerificationSession
{
    public required string Host { get; init; }

    /// <summary>cookie 名 → 值 , 回放时拼成 Cookie 请求头</summary>
    public required IReadOnlyDictionary<string, string> Cookies { get; init; }

    /// <summary>取得该会话的时刻 ( 供管线判断"是不是刚被别人过掉的" )</summary>
    public required DateTime ClearedAt { get; init; }

    public required DateTime ExpiresAt { get; init; }

    /// <summary>是否超过本地缓存上限 ( 真实有效期由服务端决定 , 过期后下一轮识别会再拦一次 )</summary>
    public bool IsExpired => DateTime.Now >= ExpiresAt;

    /// <summary>拼好的 Cookie 请求头 ( 供日志与排障 )</summary>
    public string CookieHeader => string.Join("; ", Cookies.Select(item => $"{item.Key}={item.Value}"));

    /// <summary>组装会话 : 同名 cookie 去重取末值 , TTL 为本地缓存上限</summary>
    public static VerificationSession Create(string host, IEnumerable<KeyValuePair<string, string>> cookies,
        int ttlSeconds)
    {
        var cookieMap = cookies
            .Where(item => !string.IsNullOrEmpty(item.Key))
            .GroupBy(item => item.Key)
            .ToDictionary(group => group.Key, group => group.Last().Value);
        var now = DateTime.Now;
        return new VerificationSession
        {
            Host = host,
            Cookies = cookieMap,
            ClearedAt = now,
            // TTL 只是本地缓存上限 : cookie 的真实有效期由服务端决定 , 过期了下一轮识别会再拦一次
            ExpiresAt = now.AddSeconds(ttlSeconds)
        };
    }
}
