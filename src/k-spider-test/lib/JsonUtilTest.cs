using System.Text.Json.Nodes;
using k_spider_dotnet_lib.json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace k_spider_test.lib;

[TestClass]
public class JsonUtilTest
{
    [TestMethod]
    public void GetJsonKeepChineseUnescaped()
    {
        var json = JsonUtil.GetJson(new Dictionary<string, string> { { "title", "央行宣布降准" } });
        Assert.IsTrue(json.Contains("央行宣布降准"));
    }

    [TestMethod]
    public void GetJsonRoundTrip()
    {
        var json = JsonUtil.GetJson(new Dictionary<string, object> { { "code", 1 }, { "url", "https://a.b/c" } });
        var node = JsonNode.Parse(json)!;
        Assert.AreEqual(1, (int)node["code"]!);
        Assert.AreEqual("https://a.b/c", (string?)node["url"]);
    }
}
