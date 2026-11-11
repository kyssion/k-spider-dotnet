using KSpider.Admin.Api.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace KSpider.Admin.WebHosting;

/// <summary>
///     web 静态资源与 SPA fallback。
/// </summary>
public static class WebHostingExtensions
{
    /// <summary>
    ///     启用默认文件与静态资源缓存策略。
    /// </summary>
    /// <param name="app">Web 应用。</param>
    public static IApplicationBuilder UseAdminStaticFiles(this WebApplication app)
    {
        var options = new StaticFileOptions
        {
            OnPrepareResponse = context =>
            {
                var path = context.Context.Request.Path;
                if (path.StartsWithSegments("/assets"))
                {
                    // Vite 产物文件名带内容哈希，可放心一年长缓存。
                    context.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
                }
                else if (path.Value?.EndsWith("index.html", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // 入口 HTML 必须每次回源，否则发布后仍引用旧哈希资源。
                    context.Context.Response.Headers.CacheControl = "no-cache,no-store,must-revalidate";
                }
            },
        };

        // 根路径映射 index.html，其余静态资源交给 StaticFiles。
        app.UseDefaultFiles();
        app.UseStaticFiles(options);
        return app;
    }

    /// <summary>
    ///     映射 SPA fallback；/api 与 /grpc 未命中时仍返回 404，不返回 index.html。
    /// </summary>
    /// <param name="endpoints">端点路由构建器。</param>
    public static IEndpointRouteBuilder MapSpaFallback(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapFallback(async context =>
        {
            // /api 与 /grpc 未命中必须返回 404，不能把 index.html 当作接口响应。
            if (context.Request.Path.StartsWithSegments("/api") ||
                context.Request.Path.StartsWithSegments("/grpc"))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsJsonAsync(new ApiError("not_found"));
                return;
            }

            // 其余路径视为前端客户端路由，返回 SPA 入口由 React Router 继续解析。
            var indexPath = Path.Combine(
                context.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootPath ??
                "wwwroot",
                "index.html");

            if (!File.Exists(indexPath))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.SendFileAsync(indexPath);
        }).AllowAnonymous();

        return endpoints;
    }
}
