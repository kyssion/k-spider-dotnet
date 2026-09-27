using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using KSpider.Common;
namespace KSpider.Test.Common;

/// <summary>
///     JsonTools 序列化回归 ( 全部离线 )
/// </summary>
[TestClass]
public class JsonUtilTest
{
    [TestMethod]
    public void GetJsonKeepChineseUnescaped()
    {
        var json = JsonTools.GetJson(new Dictionary<string, string> { { "title", "央行宣布降准" } });
        Assert.IsTrue(json.Contains("央行宣布降准"));
    }

    [TestMethod]
    public void GetJsonRoundTrip()
    {
        var json = JsonTools.GetJson(new Dictionary<string, object> { { "code", 1 }, { "url", "https://a.b/c" } });
        var node = JsonNode.Parse(json)!;
        Assert.AreEqual(1, (int)node["code"]!);
        Assert.AreEqual("https://a.b/c", (string?)node["url"]);
    }
}
