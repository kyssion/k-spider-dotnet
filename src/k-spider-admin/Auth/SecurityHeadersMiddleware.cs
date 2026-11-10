using Microsoft.AspNetCore.Http;

namespace KSpider.Admin.Auth;

/// <summary>
///     为所有响应补充基础安全头。
/// </summary>
/// <remarks>
///     使用 OnStarting 而不是直接写 Header，避免响应开始后被修改。CSP 与 HSTS 依赖
///     前端构建形态和反向代理配置，暂不在代码中硬编码。
/// </remarks>
/// <param name="next">下一个中间件。</param>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    /// <summary>
    ///     执行中间件。
    /// </summary>
    /// <param name="context">当前请求上下文。</param>
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            return Task.CompletedTask;
        });

        await next(context);
    }
}
