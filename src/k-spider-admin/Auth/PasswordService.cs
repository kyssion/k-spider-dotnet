using KSpider.Admin.Repository.Models;
using Microsoft.AspNetCore.Identity;

namespace KSpider.Admin.Auth;

/// <summary>
///     密码哈希与校验；封装 ASP.NET Core PasswordHasher，调用方不接触摘要细节。
/// </summary>
public sealed class PasswordService
{
    private readonly PasswordHasher<SysUserRecord> _hasher = new();

    /// <summary>
    ///     用当前框架参数哈希密码。
    /// </summary>
    /// <param name="user">账号记录；PasswordHasher 仅用于上下文。</param>
    /// <param name="password">明文密码。</param>
    /// <returns>可持久化的密码摘要。</returns>
    public string Hash(SysUserRecord user, string password)
    {
        return _hasher.HashPassword(user, password);
    }

    /// <summary>
    ///     校验明文密码与摘要是否匹配。
    /// </summary>
    /// <param name="user">账号记录。</param>
    /// <param name="password">待校验明文密码。</param>
    /// <returns>失败、成功或需要重新哈希。</returns>
    public PasswordVerificationResult Verify(SysUserRecord user, string password)
    {
        return _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
    }

    /// <summary>
    ///     为新账号生成密码摘要；不需要先构造完整账号行。
    /// </summary>
    /// <param name="username">登录名。</param>
    /// <param name="password">初始密码。</param>
    /// <returns>可持久化的密码摘要。</returns>
    public string HashForNewUser(string username, string password)
    {
        var placeholder = new SysUserRecord(0, username, string.Empty, null, "active", DateTime.UtcNow, DateTime.UtcNow);
        return Hash(placeholder, password);
    }
}
