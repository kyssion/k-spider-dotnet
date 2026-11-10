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
        app.UseMiddleware<ApiExceptionMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseAdminStaticFiles();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<CsrfMiddleware>();
        return app;
    }
}
