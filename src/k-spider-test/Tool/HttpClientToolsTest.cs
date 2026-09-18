using KSpider.Tool.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Tool;

/// <summary>
///     HttpClient 共享客户端的压缩解压配置测试 ( 离线 , 不访问网络 )
/// </summary>
[TestClass]
public class HttpClientToolsTest
{
    /// <summary>
    ///     Accept-Encoding 必须交由 handler 自动携带 ( 开启了 AutomaticDecompression ) ,
    ///     手动设置该头会使 SocketsHttpHandler 视为调用方自行处理压缩 , 自动解压随之失效 ,
    ///     对端返回 gzip/br 压缩响应时将拿到乱码。
    /// </summary>
    [TestMethod]
    public void AcceptEncodingShouldBeHandledByDecompressHandler()
    {
        // 共享客户端 : 不带手动 Accept-Encoding
        Assert.IsFalse(HttpClientTools.GetHttpClient().DefaultRequestHeaders.Contains("Accept-Encoding"));

        // 按 host 伪装客户端 : 其余伪装头保持完整 , 但 Accept-Encoding 同样不能手动设置
        var byHost = HttpClientTools.CreateByHost("finance.eastmoney.com");
        Assert.IsFalse(byHost.DefaultRequestHeaders.Contains("Accept-Encoding"));
        Assert.IsTrue(byHost.DefaultRequestHeaders.Contains("User-Agent"));
        Assert.IsTrue(byHost.DefaultRequestHeaders.Contains("Accept-Language"));

        // 同一 host 重复获取应返回同一实例 ( 缓存生效 )
        Assert.AreSame(byHost, HttpClientTools.CreateByHost("finance.eastmoney.com"));
    }
}
