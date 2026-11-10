namespace KSpider.Admin.Auth;

/// <summary>登录请求。</summary>
/// <param name="Username">登录名。</param>
/// <param name="Password">明文密码；只在请求生命周期内存在。</param>
/// <param name="RememberMe">true 时签发 7 天持久 Cookie，否则为浏览器会话 Cookie。</param>
public sealed record LoginRequest(string Username, string Password, bool RememberMe);

/// <summary>修改自己的密码。</summary>
/// <param name="CurrentPassword">当前密码。</param>
/// <param name="NewPassword">新密码，8–128 位，允许空格与 Unicode。</param>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>创建账号。</summary>
/// <param name="Username">登录名，3–32 位，字符集见 sys_user CHECK。</param>
/// <param name="Password">初始密码，8–128 位。</param>
/// <param name="DisplayName">展示名，可空。</param>
public sealed record UserCreateRequest(string Username, string Password, string? DisplayName);

/// <summary>修改账号展示名和/或状态；两者至少提供一个。</summary>
/// <param name="DisplayName">展示名；null 表示不修改。</param>
/// <param name="Status">active / disabled；null 表示不修改。</param>
public sealed record UserPatchRequest(string? DisplayName, string? Status);

/// <summary>重置其他账号密码。</summary>
/// <param name="NewPassword">新密码，8–128 位。</param>
public sealed record ResetPasswordRequest(string NewPassword);

/// <summary>账号列表/详情视图。</summary>
/// <param name="Id">账号主键。</param>
/// <param name="Username">登录名。</param>
/// <param name="DisplayName">展示名。</param>
/// <param name="Status">active / disabled。</param>
/// <param name="CreateTime">创建时间。</param>
public sealed record UserView(
    long Id,
    string Username,
    string? DisplayName,
    string Status,
    DateTime CreateTime);

/// <summary>当前登录用户视图。</summary>
/// <param name="Id">账号主键。</param>
/// <param name="Username">登录名。</param>
/// <param name="DisplayName">展示名。</param>
/// <param name="CsrfToken">当前请求的 antiforgery token；写请求通过 X-CSRF-TOKEN 回传。</param>
public sealed record MeView(
    long Id,
    string Username,
    string? DisplayName,
    string CsrfToken);
