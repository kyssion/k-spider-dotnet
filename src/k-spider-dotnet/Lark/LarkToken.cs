using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

using KSpider.Common;
namespace KSpider.Lark;

/// <summary>
///     tenant_access_token 进程内缓存载体
/// </summary>
public class TenantAccessToken
{
    public string? TokenValue;
    public DateTime BDateTime { get; set; }

    /// <summary>
    ///     token 有效期 ( 秒 ) , 飞书固定 7200
    /// </summary>
    public const int ExpireTime = 7200;

    /// <summary>
    ///     提前刷新的安全边际 ( 秒 ) , 避免临界点拿到即将过期的 token
    /// </summary>
    public const int RefreshAheadSeconds = 600;
}

/// <summary>
///     tenant_access_token 获取与缓存 : 未到临期直接复用 , 过期自动重取
/// </summary>
public class LarkToken : LarkDatasource
{
    private static readonly ILogger Logger = LogFactory.GetLogger<LarkToken>();
    private static readonly HttpClient HttpClient = new();

    private static TenantAccessToken TenantAccessToken { get; set; } = new();

    /// <summary>
    ///     取 token : 缓存未到期直接复用 , 否则请求飞书接口重取 ( 失败抛 LarkGetTokenError )
    /// </summary>
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
