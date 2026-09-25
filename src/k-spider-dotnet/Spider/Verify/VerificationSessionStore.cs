namespace KSpider.Spider.Verify;

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

    public bool IsExpired => DateTime.Now >= ExpiresAt;

    public string CookieHeader => string.Join("; ", Cookies.Select(item => $"{item.Key}={item.Value}"));

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

/// <summary>
///     已通过验证的会话缓存 , 按主机索引。过验证代价高 ( 要起浏览器 ) ,
///     所以一次通过后的会话要在进程内复用 , 而不是每个请求都过一遍。
///     读多写少、按主机隔离 , 直接锁字典即可 ( 主机数量 = 源数量 ) 。
/// </summary>
public sealed class VerificationSessionStore
{
    private readonly Dictionary<string, VerificationSession> _sessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _lock = new();

    /// <summary>取该主机当前有效会话 , 过期即移除并返回 null</summary>
    public VerificationSession? Get(string host)
    {
        if (string.IsNullOrEmpty(host)) return null;
        lock (_lock)
        {
            if (!_sessions.TryGetValue(host, out var session)) return null;
            if (!session.IsExpired) return session;
            _sessions.Remove(host);
            return null;
        }
    }

    public void Set(VerificationSession session)
    {
        lock (_lock)
        {
            _sessions[session.Host] = session;
        }
    }

    public void Remove(string host)
    {
        lock (_lock)
        {
            _sessions.Remove(host);
        }
    }

    /// <summary>当前有效会话快照 ( 供日志与排障 )</summary>
    public IReadOnlyList<VerificationSession> Snapshot()
    {
        lock (_lock)
        {
            return _sessions.Values.Where(session => !session.IsExpired).ToList();
        }
    }
}
