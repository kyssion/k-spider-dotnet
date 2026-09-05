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

    /// <summary>
    ///     提前刷新的安全边际 ( 秒 ) , 避免临界点拿到即将过期的 token
    /// </summary>
    public const int RefreshAheadSeconds = 600;
}

public class LarkToken : LarkDatasource
{
    private static readonly ILogger Logger = LogFactory.GetLogger<LarkToken>();
    private static readonly HttpClient HttpClient = new();

    private static TenantAccessToken TenantAccessToken { get; set; }

    public static async Task<string> GetTenantAccessToken(string appId, string appSecret)
    {
        if (TenantAccessToken.TokenValue != null &&
            TimeTools.GetDiffInSeconds(DateTime.Now, TenantAccessToken.BDateTime) <=
            TenantAccessToken.ExpireTime - TenantAccessToken.RefreshAheadSeconds)
            return TenantAccessToken.TokenValue;
        var content = new StringContent(
            $"{{\"app_id\":\"{appId}\",\"app_secret\":\"{appSecret}\"}}", System.Text.Encoding.UTF8, "application/json");
        var response = await HttpClient.PostAsync(TenantAccessTokenUrl, content);
        var responseBody = await response.Content.ReadAsStringAsync();
        var forecastNode = JsonNode.Parse(responseBody)!;
        var code = forecastNode["code"]?.AsValue().GetValue<int>() ?? -1;
        var token = forecastNode["tenant_access_token"]?.ToString() ?? "";
        if (code != 0 || token.Length == 0)
        {
            Logger.LogError("[GetTenantAccessToken] token code err , code {0} , msg : {1} ,  token : {2}", code,
                forecastNode["msg"]?.ToString() ?? "", token);
            throw new LarkGetTokenError();
        }

        // 整体一次性赋值 , 避免读到 "新 token + 旧时间" 的中间态
        TenantAccessToken = new TenantAccessToken { TokenValue = token, BDateTime = DateTime.Now };
        return token;
    }
}
