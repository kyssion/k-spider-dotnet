using KSpider.Admin.Repository.Models;

namespace KSpider.Admin.Repository.Abstractions;

/// <summary>
///     sys_user 数据访问边界。
/// </summary>
/// <remarks>
///     密码只以摘要形式传入；本接口不校验明文规则，也不做“最后一个 active 账号”以外的业务判断。
///     TryDisableAsync 必须用单条条件更新保证并发下仍至少保留一个 active 账号。
/// </remarks>
public interface IUserRepository
{
    /// <summary>
    ///     按用户名大小写不敏感查找。
    /// </summary>
    /// <param name="username">登录名。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<SysUserRecord?> FindByUsernameAsync(string username, CancellationToken cancellationToken);

    /// <summary>
    ///     按主键查找。
    /// </summary>
    /// <param name="id">账号主键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<SysUserRecord?> FindByIdAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    ///     列出全部账号。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<IReadOnlyList<SysUserRecord>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     创建账号并返回主键。
    /// </summary>
    /// <param name="username">登录名；调用方已规范化。</param>
    /// <param name="passwordHash">密码摘要。</param>
    /// <param name="displayName">展示名，可空。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<long> CreateAsync(
        string username,
        string passwordHash,
        string? displayName,
        CancellationToken cancellationToken);

    /// <summary>
    ///     更新展示名。
    /// </summary>
    /// <param name="id">账号主键。</param>
    /// <param name="displayName">展示名，可空。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>目标行存在并更新成功返回 true。</returns>
    Task<bool> UpdateDisplayNameAsync(long id, string? displayName, CancellationToken cancellationToken);

    /// <summary>
    ///     尝试禁用账号。
    /// </summary>
    /// <param name="id">账号主键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>目标为 active 且禁用后仍至少有一个 active 账号时返回 true。</returns>
    Task<bool> TryDisableAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    ///     启用账号。
    /// </summary>
    /// <param name="id">账号主键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>目标为 disabled 且更新成功返回 true。</returns>
    Task<bool> EnableAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    ///     更新密码摘要。
    /// </summary>
    /// <param name="id">账号主键。</param>
    /// <param name="passwordHash">新密码摘要。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>目标行存在并更新成功返回 true。</returns>
    Task<bool> UpdatePasswordHashAsync(long id, string passwordHash, CancellationToken cancellationToken);
}
