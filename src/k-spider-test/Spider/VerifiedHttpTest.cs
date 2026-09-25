using System.Net;
using System.Net.Sockets;
using System.Text;
using KSpider.Spider.Verify;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     抓取层门面 ( VerifiedHttp ) 的会话回放回归 : 起一个本地回环服务 , 断言**服务端实际收到的 Cookie 头**。
///     为什么必须看服务端收到了什么 : 会话回放原先走"手动给请求加 Cookie 头" ,
///     而 handler 的 CookieContainer 会把同名旧值追加在后面 , 请求会带两份同名 cookie ( 服务端取哪份未定义 )。
///     这种问题读代码看不出来 , 只有观测真实请求才能发现 —— 所以这里不 mock , 走真链路 ( 仅回环 , 不联网 ) 。
/// </summary>
[TestClass]
public class VerifiedHttpTest
{
    private const string Host = "127.0.0.1";

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task SessionReplaySendsOneFreshCookieWithoutDuplicates()
    {
        var received = new List<string>();
        var port = FindFreePort();
        using var server = new LoopbackServer(port, received);
        var url = $"http://{Host}:{port}/list";

        // 第一次 : 服务端下发一个同名 cookie , 让 handler 的容器里留下旧值
        await VerifiedHttp.GetStringAsync(Host, url);
        // 模拟浏览器过验证拿到的放行值落进会话缓存
        VerificationRegistry.SessionStore.Set(VerificationSession.Create(Host,
            [new KeyValuePair<string, string>("sid", "FRESH")], 60));
        try
        {
            await VerifiedHttp.GetStringAsync(Host, url);
        }
        finally
        {
            VerificationRegistry.SessionStore.Remove(Host);
        }

        foreach (var item in received) TestContext.WriteLine("服务端收到 Cookie : " + item);
        Assert.AreEqual(2, received.Count);
        // 没有会话时不许带上会话里的值
        Assert.IsFalse(received[0].Contains("FRESH"), $"无会话却带上了会话 cookie : {received[0]}");
        // 关键 : 只有一份 sid 且是会话里的新值 —— 旧值不许再出现 , 也不许拼成两份同名
        Assert.AreEqual("sid=FRESH", received[1]);
    }

    /// <summary>让操作系统分配一个空闲端口 , 避免固定端口在 CI 上撞车</summary>
    private static int FindFreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    /// <summary>本地回环服务 : 记录每次请求收到的 Cookie 头 , 并固定下发一个同名旧 cookie</summary>
    private sealed class LoopbackServer : IDisposable
    {
        private readonly Task _loop;
        private readonly HttpListener _listener = new();

        public LoopbackServer(int port, List<string> received)
        {
            _listener.Prefixes.Add($"http://{Host}:{port}/");
            _listener.Start();
            _loop = Task.Run(async () =>
            {
                try
                {
                    while (_listener.IsListening)
                    {
                        var context = await _listener.GetContextAsync();
                        received.Add(context.Request.Headers["Cookie"] ?? "(none)");
                        context.Response.Headers.Add("Set-Cookie", "sid=STALE; Path=/");
                        context.Response.ContentType = "application/json";
                        var bytes = Encoding.UTF8.GetBytes("""{"errno":0,"msg":"","data":{"roll_data":[]}}""");
                        context.Response.ContentLength64 = bytes.Length;
                        await context.Response.OutputStream.WriteAsync(bytes);
                        context.Response.Close();
                    }
                }
                catch (HttpListenerException)
                {
                    // 停机时 GetContextAsync 被中止 , 预期
                }
                catch (ObjectDisposedException)
                {
                    // 同上
                }
            });
        }

        public void Dispose()
        {
            _listener.Stop();
            _listener.Close();
            _loop.Wait(TimeSpan.FromSeconds(5));
        }
    }
}
