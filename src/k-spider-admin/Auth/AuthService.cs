using System.Globalization;
using System.Security.Claims;
using KSpider.Admin.Common.Logging;
using KSpider.Admin.Repository.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KSpider.Admin.Auth;

/// <summary>
///     登录结果。
/// </summary>
public enum LoginOutcome
{
    /// <summary>登录成功，已签发 Cookie。</summary>
    Success,

    /// <summary>用户名、密码或账号状态不满足登录条件；对外统一返回 401。</summary>
    InvalidCredentials,

    /// <summary>Origin/Referer 校验失败。</summary>
    OriginRejected,
}

/// <summary>
///     修改密码结果。
/// </summary>
public enum ChangePasswordOutcome
{
    /// <summary>密码已更新。</summary>
    Success,

    /// <summary>账号不存在。</summary>
    NotFound,

    /// <summary>当前密码错误。</summary>
    InvalidCurrentPassword,

    /// <summary>新密码不满足长度规则。</summary>
    InvalidNewPassword,
}

/// <summary>
///     登录、登出与密码变更。
/// </summary>
/// <remarks>
///     <para>
///         登录先校验 Origin/Referer，再查账号、校验密码摘要；摘要命中 SuccessRehashNeeded 时
///         用当前参数重新哈希。成功后签发 Cookie 会话，勾选 rememberMe 时写 7 天持久 Cookie。
///     </para>
///     <para>
///         登录失败对外不区分具体原因；日志可记录原因但绝不能记录密码。
///     </para>
/// </remarks>
/// <param name="users">账号仓储。</param>
/// <param name="passwords">密码哈希服务。</param>
/// <param name="options">认证配置。</param>
/// <param name="environment">宿主环境；开发期允许无 Origin 的非浏览器调用。</param>
/// <param name="logger">结构化日志。</param>
public sealed class AuthService(
    IUserRepository users,
    PasswordService passwords,
    IOptions<AuthOptions> options,
    IHostEnvironment environment,
    ILogger<AuthService> logger)
{
    private readonly AuthOptions _options = options.Value;

    /// <summary>
    ///     校验登录来源并签发 Cookie。
    /// </summary>
    /// <param name="context">当前 HTTP 上下文，用于读取 Origin/Referer 与写入 Cookie。</param>
    /// <param name="request">登录请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>登录结果。</returns>
    public async Task<LoginOutcome> LoginAsync(
        HttpContext context,
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsAllowedOrigin(context.Request))
        {
            AdminLog.AuthLoginFailed(logger, request.Username, "origin");
            return LoginOutcome.OriginRejected;
        }

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            AdminLog.AuthLoginFailed(logger, request.Username, "empty_credentials");
            return LoginOutcome.InvalidCredentials;
        }

        var user = await users.FindByUsernameAsync(request.Username.Trim(), cancellationToken);
        if (user is null || !string.Equals(user.Status, "active", StringComparison.Ordinal))
        {
            AdminLog.AuthLoginFailed(logger, request.Username, "user_or_status");
            return LoginOutcome.InvalidCredentials;
        }

        var verification = passwords.Verify(user, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            AdminLog.AuthLoginFailed(logger, request.Username, "password");
            return LoginOutcome.InvalidCredentials;
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            var rehashed = passwords.Hash(user, request.Password);
            await users.UpdatePasswordHashAsync(user.Id, rehashed, cancellationToken);
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Name, user.Username),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties
        {
            IsPersistent = request.RememberMe,
            AllowRefresh = false,
        };

        if (request.RememberMe)
        {
            properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(_options.RememberMeDays);
        }

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties);

        AdminLog.AuthLoginSucceeded(logger, user.Id, user.Username);
        return LoginOutcome.Success;
    }

    /// <summary>
    ///     修改当前账号密码。
    /// </summary>
    /// <param name="userId">当前账号主键。</param>
    /// <param name="request">当前密码与新密码。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>修改结果；成功后只更新摘要，不强制注销已有会话。</returns>
    public async Task<ChangePasswordOutcome> ChangePasswordAsync(
        long userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ChangePasswordOutcome.NotFound;
        }

        if (!IsValidPassword(request.NewPassword))
        {
            return ChangePasswordOutcome.InvalidNewPassword;
        }

        if (string.IsNullOrEmpty(request.CurrentPassword) ||
            passwords.Verify(user, request.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            return ChangePasswordOutcome.InvalidCurrentPassword;
        }

        var hash = passwords.Hash(user, request.NewPassword);
        var updated = await users.UpdatePasswordHashAsync(user.Id, hash, cancellationToken);
        if (!updated)
        {
            return ChangePasswordOutcome.NotFound;
        }

        AdminLog.AuthAccountChanged(logger, userId, userId, "change_password");
        return ChangePasswordOutcome.Success;
    }

    /// <summary>
    ///     清除当前会话 Cookie。
    /// </summary>
    /// <param name="context">当前 HTTP 上下文。</param>
    public Task SignOutAsync(HttpContext context)
    {
        return context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    /// <summary>
    ///     校验新密码长度规则：8–128 位，允许空格与 Unicode。
    /// </summary>
    /// <param name="password">待校验密码。</param>
    public static bool IsValidPassword(string? password)
    {
        return password is { Length: >= 8 and <= 128 };
    }

    /// <summary>
    ///     校验登录请求来源。
    /// </summary>
    /// <remarks>
    ///     优先比较 Origin；缺失时回退 Referer。开发期允许两者都缺失，便于 curl/本地测试；
    ///     生产必须显式携带并命中白名单或当前 Host。
    /// </remarks>
    private bool IsAllowedOrigin(HttpRequest request)
    {
        var origin = request.Headers.Origin.ToString();
        if (!string.IsNullOrWhiteSpace(origin))
        {
            return MatchesOrigin(request, origin);
        }

        var referer = request.Headers.Referer.ToString();
        if (!string.IsNullOrWhiteSpace(referer) &&
            Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
        {
            return MatchesOrigin(request, $"{refererUri.Scheme}://{refererUri.Authority}");
        }

        return environment.IsDevelopment();
    }

    private bool MatchesOrigin(HttpRequest request, string origin)
    {
        var normalized = origin.TrimEnd('/');
        if (_options.AllowedOrigins.Any(allowed =>
                string.Equals(allowed.TrimEnd('/'), normalized, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return Uri.TryCreate(normalized, UriKind.Absolute, out var uri) &&
               string.Equals(uri.Authority, request.Host.Value, StringComparison.OrdinalIgnoreCase);
    }
}
