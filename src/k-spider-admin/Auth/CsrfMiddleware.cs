using KSpider.Admin.Api.Contracts;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace KSpider.Admin.Auth;

/// <summary>
///     除登录外，所有 /api 写方法都必须携带并校验 antiforgery token。
/// </summary>
/// <remarks>
///     登录不校验 CSRF，是因为此时还没有会话与 token；登录改由 Origin/Referer 校验防跨站。
///     其他写方法即使未登录也会先经过授权中间件，不会到达本校验。
/// </remarks>
/// <param name="next">下一个中间件。</param>
public sealed class CsrfMiddleware(RequestDelegate next)
{
    /// <summary>
    ///     执行 CSRF 校验。
    /// </summary>
    /// <param name="context">当前请求上下文。</param>
    /// <param name="antiforgery">ASP.NET Core antiforgery 服务。</param>
    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        if (RequiresValidation(context.Request))
        {
            try
            {
                await antiforgery.ValidateRequestAsync(context);
            }
            catch (AntiforgeryValidationException)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new ApiError("invalid_csrf"));
                return;
            }
        }

        await next(context);
    }

    /// <summary>
    ///     判断请求是否需要 CSRF 校验。
    /// </summary>
    /// <param name="request">当前请求。</param>
    /// <returns>需要校验返回 true。</returns>
    private static bool RequiresValidation(HttpRequest request)
    {
        if (!request.Path.StartsWithSegments("/api"))
        {
            return false;
        }

        if (request.Path.StartsWithSegments("/api/auth/login"))
        {
            return false;
        }

        return HttpMethods.IsPost(request.Method) ||
               HttpMethods.IsPut(request.Method) ||
               HttpMethods.IsPatch(request.Method) ||
               HttpMethods.IsDelete(request.Method);
    }
}
