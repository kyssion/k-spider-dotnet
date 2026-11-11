using System.Security.Claims;
using KSpider.Admin.Api.Contracts;
using KSpider.Admin.Repository.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KSpider.Admin.Auth;

/// <summary>
///     认证服务注册：Cookie 认证、授权、Antiforgery、Data Protection 与账号服务。
/// </summary>
public static class AuthRegistration
{
    /// <summary>
    ///     注册 admin 认证与账号管理依赖。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configuration">应用配置。</param>
    public static IServiceCollection AddAdminAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Cookie 名称等认证可调参数；启动时校验，避免配置写错后才在登录时暴露。
        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 使用 Cookie 认证作为唯一登录态；API 与 SPA 共用同一份 HttpOnly Cookie。
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                // Cookie 只存不透明票据，禁止脚本读取；Strict 降低跨站携带风险。
                options.Cookie.Name = configuration[$"{AuthOptions.SectionName}:CookieName"] ?? "kspider.auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.Path = "/";
                options.Cookie.IsEssential = true;
                // 固定 8 小时过期且不滑动，避免活跃窗口无限续期。
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = false;
                // SPA 路由；/api 未登录由下面的重定向事件改成 401 JSON。
                options.LoginPath = "/login";
                options.AccessDeniedPath = "/login";
                options.Events = new CookieAuthenticationEvents
                {
                    // 每次请求回查账号状态，账号被禁用或删除后旧 Cookie 立即失效。
                    OnValidatePrincipal = ValidatePrincipalAsync,
                    // API 返回 401 JSON；页面跳登录并携带 returnUrl。
                    OnRedirectToLogin = RedirectToLoginAsync,
                    // API 返回 403 JSON，页面统一回登录页。
                    OnRedirectToAccessDenied = RedirectToAccessDeniedAsync,
                };
            });

        // 默认策略要求所有端点已认证；公开端点必须显式 AllowAnonymous()。
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        // CSRF 双提交：前端从 /api/auth/me 取 token，写请求通过 X-CSRF-TOKEN 头带回。
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "kspider.csrf";
            options.Cookie.HttpOnly = false;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });

        // Data Protection 密钥决定 Cookie 能否跨进程/重启解密；生产必须持久化到共享目录。
        var keysPath = configuration["DataProtection:KeysPath"];
        if (!string.IsNullOrWhiteSpace(keysPath))
        {
            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }
        else
        {
            // 未配置时使用进程内临时密钥；重启后旧登录态失效，仅适合开发环境。
            services.AddDataProtection();
        }

        // 账号能力：密码哈希校验、登录/登出/改密、账号管理与展示名维护。
        services.AddSingleton<PasswordService>();
        services.AddSingleton<AuthService>();
        services.AddSingleton<UserService>();

        return services;
    }

    /// <summary>
    ///     每次请求校验 Cookie 里的用户仍存在且为 active；禁用/删除后立即踢下线。
    /// </summary>
    private static async Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
    {
        // 以数据库为准，不信任 Cookie 中可能过期的用户状态。
        var userRepository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
        var idValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(idValue, out var userId))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        var user = await userRepository.FindByIdAsync(userId, context.HttpContext.RequestAborted);
        if (user is null || !string.Equals(user.Status, "active", StringComparison.Ordinal))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }

    /// <summary>
    ///     未登录时的分流：API 返回 401 JSON，SPA 跳登录页并保留 returnUrl。
    /// </summary>
    private static async Task RedirectToLoginAsync(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ApiError("unauthorized"));
            return;
        }

        // 回跳地址只取本地 Path+Query，避免把外部地址塞进 returnUrl。
        var returnUrl = context.Request.Path + context.Request.QueryString;
        context.Response.Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    /// <summary>
    ///     已登录但无权限时的分流；当前所有登录用户同权，主要作为后续扩展兜底。
    /// </summary>
    private static async Task RedirectToAccessDeniedAsync(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            await context.Response.WriteAsJsonAsync(new ApiError("forbidden"));
        }
    }
}
