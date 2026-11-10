using System.Globalization;
using System.Security.Claims;

namespace KSpider.Admin.Auth;

public static class CurrentUser
{
    public static long GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : 0;
    }
}
