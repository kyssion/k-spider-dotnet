namespace KSpider.Admin.Repository.Models;

/// <summary>
///     sys_user 行快照。
/// </summary>
/// <param name="Id">账号主键。</param>
/// <param name="Username">登录名。</param>
/// <param name="PasswordHash">密码摘要；绝不记录或返回明文。</param>
/// <param name="DisplayName">展示名。</param>
/// <param name="Status">active / disabled。</param>
/// <param name="CreateTime">创建时间。</param>
/// <param name="UpdateTime">更新时间。</param>
public sealed record SysUserRecord(
    long Id,
    string Username,
    string PasswordHash,
    string? DisplayName,
    string Status,
    DateTime CreateTime,
    DateTime UpdateTime);
