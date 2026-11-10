using System.Text.RegularExpressions;
using KSpider.Admin.Common.Logging;
using KSpider.Admin.Repository.Abstractions;
using KSpider.Admin.Repository.Exceptions;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace KSpider.Admin.Auth;

/// <summary>
///     账号管理结果。
/// </summary>
public enum UserMutationOutcome
{
    /// <summary>操作成功。</summary>
    Success,

    /// <summary>输入不满足长度、字符集或状态取值约束。</summary>
    ValidationError,

    /// <summary>目标账号不存在。</summary>
    NotFound,

    /// <summary>并发冲突或违反“至少保留一个 active 账号”约束。</summary>
    Conflict,

    /// <summary>禁止操作自己（禁用/重置密码）。</summary>
    Forbidden,
}

/// <summary>
///     账号创建、展示名修改、启停与密码重置。
/// </summary>
/// <remarks>
///     <para>
///         所有登录用户同权；管理操作写应用日志。禁用自己、禁用最后一个 active 账号、
///         通过账号管理接口重置自己密码都会被拒绝。
///     </para>
///     <para>
///         “最后一个 active 账号”的并发保护在仓储的单条条件 UPDATE 中完成，而不是在本服务
///         先查后写；本服务只把失败映射为 Conflict。
///     </para>
/// </remarks>
/// <param name="users">账号仓储。</param>
/// <param name="passwords">密码哈希服务。</param>
/// <param name="logger">结构化日志。</param>
public sealed class UserService(
    IUserRepository users,
    PasswordService passwords,
    ILogger<UserService> logger)
{
    private static readonly Regex UsernamePattern = new(
        "^[a-zA-Z0-9._-]+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>
    ///     读取单个账号视图。
    /// </summary>
    /// <param name="id">账号主键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>账号视图；不存在时返回 null。</returns>
    public async Task<UserView?> GetAsync(long id, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(id, cancellationToken);
        return user is null
            ? null
            : new UserView(user.Id, user.Username, user.DisplayName, user.Status, user.CreateTime);
    }

    /// <summary>
    ///     列出全部账号。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<IReadOnlyList<UserView>> ListAsync(CancellationToken cancellationToken)
    {
        var records = await users.ListAsync(cancellationToken);
        return records
            .Select(user => new UserView(
                user.Id,
                user.Username,
                user.DisplayName,
                user.Status,
                user.CreateTime))
            .ToList();
    }

    /// <summary>
    ///     创建账号。
    /// </summary>
    /// <remarks>
    ///     用户名按 DDL 的字符集与长度约束校验；密码长度 8–128。先查重用于快速返回，
    ///     最终唯一性仍由数据库唯一索引兜底，捕获唯一冲突后返回 Conflict。
    /// </remarks>
    /// <param name="request">用户名、初始密码与展示名。</param>
    /// <param name="operatorId">操作者账号，用于审计日志。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<UserMutationOutcome> CreateAsync(
        UserCreateRequest request,
        long operatorId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            !IsValidUsername(request.Username) ||
            !AuthService.IsValidPassword(request.Password) ||
            !IsValidDisplayName(request.DisplayName))
        {
            return UserMutationOutcome.ValidationError;
        }

        var username = request.Username.Trim();
        if (await users.FindByUsernameAsync(username, cancellationToken) is not null)
        {
            return UserMutationOutcome.Conflict;
        }

        var hash = passwords.HashForNewUser(username, request.Password);
        long userId;
        try
        {
            userId = await users.CreateAsync(
                username,
                hash,
                NormalizeDisplayName(request.DisplayName),
                cancellationToken);
        }
        catch (KDbException exception) when (IsUniqueViolation(exception))
        {
            return UserMutationOutcome.Conflict;
        }

        AdminLog.AuthAccountChanged(logger, operatorId, userId, "create");
        return UserMutationOutcome.Success;
    }

    /// <summary>
    ///     修改展示名和/或启停状态。
    /// </summary>
    /// <param name="targetId">目标账号主键。</param>
    /// <param name="request">至少包含 displayName 或 status 之一。</param>
    /// <param name="operatorId">操作者账号，用于审计与自禁用判断。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<UserMutationOutcome> PatchAsync(
        long targetId,
        UserPatchRequest request,
        long operatorId,
        CancellationToken cancellationToken)
    {
        if (request.DisplayName is null && request.Status is null)
        {
            return UserMutationOutcome.ValidationError;
        }

        if (request.Status is not null &&
            !string.Equals(request.Status, "active", StringComparison.Ordinal) &&
            !string.Equals(request.Status, "disabled", StringComparison.Ordinal))
        {
            return UserMutationOutcome.ValidationError;
        }

        if (!IsValidDisplayName(request.DisplayName))
        {
            return UserMutationOutcome.ValidationError;
        }

        var target = await users.FindByIdAsync(targetId, cancellationToken);
        if (target is null)
        {
            return UserMutationOutcome.NotFound;
        }

        if (string.Equals(request.Status, "disabled", StringComparison.Ordinal))
        {
            if (targetId == operatorId)
            {
                return UserMutationOutcome.Forbidden;
            }

            if (string.Equals(target.Status, "active", StringComparison.Ordinal) &&
                !await users.TryDisableAsync(targetId, cancellationToken))
            {
                // 仓储单条 UPDATE 同时检查“还有其他 active 账号”。
                return UserMutationOutcome.Conflict;
            }
        }
        else if (string.Equals(request.Status, "active", StringComparison.Ordinal) &&
                 string.Equals(target.Status, "disabled", StringComparison.Ordinal) &&
                 !await users.EnableAsync(targetId, cancellationToken))
        {
            return UserMutationOutcome.Conflict;
        }

        if (request.DisplayName is not null &&
            !await users.UpdateDisplayNameAsync(
                targetId,
                NormalizeDisplayName(request.DisplayName),
                cancellationToken))
        {
            return UserMutationOutcome.NotFound;
        }

        var action = request.Status is null ? "update_display_name" : $"status_{request.Status}";
        AdminLog.AuthAccountChanged(logger, operatorId, targetId, action);
        return UserMutationOutcome.Success;
    }

    /// <summary>
    ///     重置其他账号密码。
    /// </summary>
    /// <remarks>
    ///     自己改密必须走 /api/auth/change-password，以强制校验当前密码；本接口拒绝目标等于操作者。
    /// </remarks>
    /// <param name="targetId">目标账号主键。</param>
    /// <param name="newPassword">新密码。</param>
    /// <param name="operatorId">操作者账号，用于审计与自重置判断。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<UserMutationOutcome> ResetPasswordAsync(
        long targetId,
        string newPassword,
        long operatorId,
        CancellationToken cancellationToken)
    {
        if (targetId == operatorId)
        {
            return UserMutationOutcome.Forbidden;
        }

        if (!AuthService.IsValidPassword(newPassword))
        {
            return UserMutationOutcome.ValidationError;
        }

        var target = await users.FindByIdAsync(targetId, cancellationToken);
        if (target is null)
        {
            return UserMutationOutcome.NotFound;
        }

        var hash = passwords.Hash(target, newPassword);
        if (!await users.UpdatePasswordHashAsync(targetId, hash, cancellationToken))
        {
            return UserMutationOutcome.NotFound;
        }

        AdminLog.AuthAccountChanged(logger, operatorId, targetId, "reset_password");
        return UserMutationOutcome.Success;
    }

    private static bool IsValidUsername(string username)
    {
        var trimmed = username.Trim();
        return trimmed.Length is >= 3 and <= 32 && UsernamePattern.IsMatch(trimmed);
    }

    private static bool IsValidDisplayName(string? displayName)
    {
        return displayName is null || displayName.Length <= 100;
    }

    private static string? NormalizeDisplayName(string? displayName)
    {
        return string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
    }

    /// <summary>
    ///     判断是否为 PostgreSQL 唯一约束冲突；兼容 KDbException 的多层包装。
    /// </summary>
    private static bool IsUniqueViolation(KDbException exception)
    {
        Exception? current = exception;
        while (current is not null)
        {
            if (current is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }
}
