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
        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = configuration[$"{AuthOptions.SectionName}:CookieName"] ?? "kspider.auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.Path = "/";
                options.Cookie.IsEssential = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = false;
                options.LoginPath = "/login";
                options.AccessDeniedPath = "/login";
                options.Events = new CookieAuthenticationEvents
                {
                    OnValidatePrincipal = ValidatePrincipalAsync,
                    OnRedirectToLogin = RedirectToLoginAsync,
                    OnRedirectToAccessDenied = RedirectToAccessDeniedAsync,
                };
            });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "kspider.csrf";
            options.Cookie.HttpOnly = false;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });

        var keysPath = configuration["DataProtection:KeysPath"];
        if (!string.IsNullOrWhiteSpace(keysPath))
        {
            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }
        else
        {
            services.AddDataProtection();
        }

        services.AddSingleton<PasswordService>();
        services.AddSingleton<AuthService>();
        services.AddSingleton<UserService>();

        return services;
    }

    private static async Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
    {
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

    private static async Task RedirectToLoginAsync(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ApiError("unauthorized"));
            return;
        }

        var returnUrl = context.Request.Path + context.Request.QueryString;
        context.Response.Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    private static async Task RedirectToAccessDeniedAsync(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            await context.Response.WriteAsJsonAsync(new ApiError("forbidden"));
        }
    }
}
