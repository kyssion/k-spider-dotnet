using System.Text.Json.Nodes;
using k_spider_dotnet_lib.logger;
using k_spider_dotnet_lib.time;
using Microsoft.Extensions.Logging;

namespace k_spider_dotnet_lib.lark;

public struct TenantAccessToken
{
    public volatile string TokenValue;
    public DateTime BDateTime { get; set; }
    public const int ExpireTime = 7200;
}

public class LarkToken : LarkDatasource 
{
    private static readonly ILogger Logger = LogFactory.GetLogger<LarkToken>();

    private static TenantAccessToken TenantAccessToken { get; set; }

    public static async Task<string> GetTenantAccessToken(string appId , string appSecret)
    {
        if (TimeTools.GetDiffInSeconds(DateTime.Now, TenantAccessToken.BDateTime) <= TenantAccessToken.ExpireTime)
            return TenantAccessToken.TokenValue;
        var content = new StringContent($"{{\"app_id\":\"{appId}\",\"app_secret\":\"{appSecret}\"}}", System.Text.Encoding.UTF8, "application/json");
        var response = await new HttpClient().PostAsync(TenantAccessTokenUrl,content);
        var responseBody = await response.Content.ReadAsStringAsync();
        var forecastNode = JsonNode.Parse(responseBody)!;
        var code = forecastNode["code"]?.AsValue().GetValue<int>()??-1;
        var token = forecastNode["tenant_access_token"]?.ToString() ?? "";
        if (code != 0 || token.Length == 0)
        {
            Logger.LogError("[GetTenantAccessToken] token code err , code {0} , msg : {1} ,  token : {2}",code,forecastNode["msg"]?.ToString()??"",token);
            throw new LarkGetTokenError();
        }
        TenantAccessToken = TenantAccessToken with { TokenValue = token };
        TenantAccessToken = TenantAccessToken with { BDateTime = DateTime.Now };

        return TenantAccessToken.TokenValue;
    }
}