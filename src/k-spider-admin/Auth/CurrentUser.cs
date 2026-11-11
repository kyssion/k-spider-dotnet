using System.Globalization;
using System.Security.Claims;

namespace KSpider.Admin.Auth;

/// <summary>
///     从 ClaimsPrincipal 读取当前账号信息。
/// </summary>
public static class CurrentUser
{
    /// <summary>
    ///     读取当前账号主键；缺失或格式错误时返回 0，由端点决定 401。
    /// </summary>
    /// <param name="principal">当前请求的用户主体。</param>
    /// <returns>账号主键；无法解析时为 0。</returns>
    public static long GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : 0;
    }
}
