


using System.Text.Json.Serialization;
using k_spider_dotnet.tool.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public struct TemplateData
{
    [JsonPropertyName("template_id")]
    public string TemplateId { get; set; }
    [JsonPropertyName("template_variable")]
    public string TemplateVariable { get; set; }
}
public struct TemplateInfo()
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "template";

    [JsonPropertyName("data")]
    public TemplateData Data { get; set; }
}

struct MessageData
{
    [JsonPropertyName("receive_id")]
    public string ReceiveId { get; set; }
    [JsonPropertyName("msg_type")]
    public string MsgType { get; set; }
    [JsonPropertyName("content")]
    public string Content { get; set; }
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; }
}
[TestClass]
public class Testjson
{
    [TestMethod]
    public void TestIdidi()
    {
        var item = new TemplateInfo
        {
            Type = "1234",
            Data = new TemplateData
            {
                TemplateId = "1",
                TemplateVariable = "2"
            }
        };
        Console.WriteLine(JsonUtil.GetJson(item));
    }
}