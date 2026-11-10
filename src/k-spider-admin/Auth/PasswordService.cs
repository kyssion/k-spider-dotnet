using KSpider.Admin.Repository.Models;
using Microsoft.AspNetCore.Identity;

namespace KSpider.Admin.Auth;

public sealed class PasswordService
{
    private readonly PasswordHasher<SysUserRecord> _hasher = new();

    public string Hash(SysUserRecord user, string password)
    {
        return _hasher.HashPassword(user, password);
    }

    public PasswordVerificationResult Verify(SysUserRecord user, string password)
    {
        return _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
    }

    public string HashForNewUser(string username, string password)
    {
        var placeholder = new SysUserRecord(0, username, string.Empty, null, "active", DateTime.UtcNow, DateTime.UtcNow);
        return Hash(placeholder, password);
    }
}
