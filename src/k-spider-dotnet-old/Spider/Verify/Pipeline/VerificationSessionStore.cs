using KSpider.Spider.Verify.Model;

namespace KSpider.Spider.Verify.Pipeline;

/// <summary>
///     已通过验证的会话缓存 , 按主机索引。过验证代价高 ( 要起浏览器 ) ,
///     所以一次通过后的会话要在进程内复用 , 而不是每个请求都过一遍。
///     读多写少、按主机隔离 , 直接锁字典即可 ( 主机数量 = 源数量 ) 。
/// </summary>
public sealed class VerificationSessionStore
{
    /// <summary>主机 → 当前有效会话</summary>
    private readonly Dictionary<string, VerificationSession> _sessions = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>保护 _sessions 的锁 ( 读多写少 , 主机数量 = 源数量 )</summary>
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

    /// <summary>写入 / 覆盖该主机的会话 ( 过验证成功时调用 )</summary>
    public void Set(VerificationSession session)
    {
        lock (_lock)
        {
            _sessions[session.Host] = session;
        }
    }

    /// <summary>移除该主机的会话 ( 被 cookie 类验证拦住 = 会话已被服务端拒绝 , 由管线调用 )</summary>
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
