using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using k_spider_dotnet.json;
using k_spider_dotnet.logger;
using Microsoft.Extensions.Logging;

namespace k_spider_dotnet.lark;
 
public class LarkMessage : LarkToken
{
    private static readonly ILogger Logger = LogFactory.GetLogger<LarkToken>();
    private static readonly HttpClient SharedHttpClient = new();

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

    public struct TemplateData
    {
        [JsonPropertyName("template_id")]
        public string TemplateId;
        [JsonPropertyName("template_variable")]
        public Dictionary<string,object> TemplateVariable;
    }
    public struct TemplateInfo
    {
        [JsonPropertyName("type")]
        public string Type = "template";
        [JsonPropertyName("data")]
        public TemplateData Data;
        public TemplateInfo()
        {
        }
    }

    public static async Task SendTemplateMessage(string appId, string appSecret, string receiveIdType, string receiveId,
        TemplateInfo templateInfo)
    {
         await SendMessage(appId, appSecret, receiveIdType, receiveId, "interactive", JsonUtil.GetJson(templateInfo));
    }
    public static async Task SendMessage(string appId , string appSecret ,string receiveIdType, string receiveId, string msgType, string content)
    {
        var token =await GetTenantAccessToken(appId, appSecret);
        var httpContent = new StringContent(JsonUtil.GetJson(new MessageData
        {
            ReceiveId = receiveId,
            MsgType = msgType,
            Content = content,
            Uuid = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)+Random.Shared.Next()
        }), System.Text.Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{SendMessageUrl}?receive_id_type={receiveIdType}");
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Content = httpContent;
        var response = await SharedHttpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();
        var forecastNode = JsonNode.Parse(responseBody)!;
        var code = forecastNode["code"]?.AsValue().GetValue<int>()??-1;
        if (code == 0 && token.Length != 0) return;
        Logger.LogError("[SendMessage] send err , code {0} , msg : {1} , receiveId : {2}",code,forecastNode["msg"]?.ToString()??"",receiveId);
        throw new LarkGetTokenError();
    }
}