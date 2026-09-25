using KSpider.Spider.Verify;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     已通过验证的会话缓存回归 ( 全部离线 ) : 过验证代价高 , 缓存错了要么白起浏览器 , 要么把过期 cookie 发出去
/// </summary>
[TestClass]
public class VerificationSessionStoreTest
{
    private const string Host = "www.cls.cn";

    private static VerificationSession NewSession(int ttlSeconds, params (string Name, string Value)[] cookies)
    {
        return VerificationSession.Create(Host, cookies.Select(cookie => new KeyValuePair<string, string>(cookie.Name, cookie.Value)), ttlSeconds);
    }

    [TestMethod]
    public void SetThenGetReturnsSession()
    {
        var store = new VerificationSessionStore();
        store.Set(NewSession(60, ("sid", "abc")));

        var session = store.Get(Host);

        Assert.IsNotNull(session);
        Assert.AreEqual("sid=abc", session.CookieHeader);
    }

    [TestMethod]
    public void HostLookupIsCaseInsensitive()
    {
        var store = new VerificationSessionStore();
        store.Set(NewSession(60, ("sid", "abc")));

        Assert.IsNotNull(store.Get("WWW.CLS.CN"));
    }

    [TestMethod]
    public void ExpiredSessionIsDroppedOnRead()
    {
        var store = new VerificationSessionStore();
        // TTL 为负 = 会话一建立就过期 , 用来验证读时清理
        store.Set(NewSession(-1, ("sid", "abc")));

        Assert.IsNull(store.Get(Host));
        Assert.AreEqual(0, store.Snapshot().Count);
    }

    [TestMethod]
    public void RemoveClearsSession()
    {
        var store = new VerificationSessionStore();
        store.Set(NewSession(60, ("sid", "abc")));

        store.Remove(Host);

        Assert.IsNull(store.Get(Host));
    }

    [TestMethod]
    public void UnknownHostReturnsNull()
    {
        var store = new VerificationSessionStore();

        Assert.IsNull(store.Get("never.cleared.cn"));
        Assert.IsNull(store.Get(""));
    }

    [TestMethod]
    public void CookieHeaderJoinsAllCookiesAndKeepsLastDuplicate()
    {
        var session = NewSession(60, ("a", "1"), ("b", "2"), ("a", "3"));

        Assert.AreEqual("a=3; b=2", session.CookieHeader);
    }
}
