using System.ComponentModel.DataAnnotations;

namespace KSpider.Admin.Auth;

/// <summary>
///     认证与 Cookie 配置。
/// </summary>
public sealed class AuthOptions
{
    /// <summary>配置节名。</summary>
    public const string SectionName = "Auth";

    /// <summary>会话 Cookie 名。</summary>
    [Required(AllowEmptyStrings = false)]
    public string CookieName { get; set; } = "kspider.auth";

    /// <summary>“7 天免登录”持久 Cookie 天数。</summary>
    [Range(1, 30)]
    public int RememberMeDays { get; set; } = 7;

    /// <summary>
    ///     登录 Origin/Referer 白名单；开发期通常包含 Vite 开发服务器源。
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];
}
