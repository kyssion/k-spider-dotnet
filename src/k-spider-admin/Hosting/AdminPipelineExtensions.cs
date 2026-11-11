using KSpider.Admin.Api;
using KSpider.Admin.Auth;
using KSpider.Admin.WebHosting;
using Microsoft.AspNetCore.Builder;

namespace KSpider.Admin.Hosting;

/// <summary>
///     admin HTTP 管道。
/// </summary>
public static class AdminPipelineExtensions
{
    /// <summary>
    ///     安装异常边界、安全头、静态文件、认证、授权与 CSRF。
    /// </summary>
    /// <param name="app">Web 应用。</param>
    public static WebApplication UseAdminPipeline(this WebApplication app)
    {
        // 最外层异常边界：把未处理异常统一转成 ApiError，避免泄露堆栈。
        app.UseMiddleware<ApiExceptionMiddleware>();
        // 为所有响应补安全头（Referrer-Policy、X-Content-Type-Options 等）。
        app.UseMiddleware<SecurityHeadersMiddleware>();
        // 托管 wwwroot 构建产物；assets 长缓存，index.html 禁止缓存。
        app.UseAdminStaticFiles();
        // 解析认证 Cookie，恢复 HttpContext.User。
        app.UseAuthentication();
        // 执行授权策略；未标记 AllowAnonymous 的端点默认要求登录。
        app.UseAuthorization();
        // 写请求校验 CSRF，防止 Cookie 认证被跨站利用。
        app.UseMiddleware<CsrfMiddleware>();
        return app;
    }
}
