using System.Data.Common;
using KSpider.Admin.Repository.Abstractions;
using KSpider.Admin.Repository.Models;
using LinqToDB;
using LinqToDB.Data;

namespace KSpider.Admin.Repository.Postgres;

/// <summary>
///     sys_user 数据访问；最后 active 账号保护在单条条件 UPDATE 中完成。
/// </summary>
internal sealed class PostgresUserRepository(IAdminDbConnectionFactory connectionFactory)
    : PostgresRepositoryBase(connectionFactory), IUserRepository
{
    private const string UserColumns =
        "id, username, password_hash, display_name, status, create_time, update_time";


    public Task<SysUserRecord?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var sql = $"SELECT {UserColumns} FROM sys_user WHERE lower(username)=lower(@username::text)";
        return ExecuteAsync(async db =>
        {
            var rows = await db.QueryToListAsync(
                ReadUser,
                sql,
                cancellationToken,
                Parameter("username", username));
            return rows.Count > 0 ? rows[0] : null;
        }, cancellationToken);
    }

    public Task<SysUserRecord?> FindByIdAsync(long id, CancellationToken cancellationToken)
    {
        var sql = $"SELECT {UserColumns} FROM sys_user WHERE id=@id::bigint";
        return ExecuteAsync(async db =>
        {
            var rows = await db.QueryToListAsync(
                ReadUser,
                sql,
                cancellationToken,
                Parameter("id", id));
            return rows.Count > 0 ? rows[0] : null;
        }, cancellationToken);
    }

    public Task<IReadOnlyList<SysUserRecord>> ListAsync(CancellationToken cancellationToken)
    {
        var sql = $"SELECT {UserColumns} FROM sys_user ORDER BY id";
        return ExecuteAsync(
            async db => (IReadOnlyList<SysUserRecord>)await db.QueryToListAsync(
                ReadUser,
                sql,
                cancellationToken),
            cancellationToken);
    }

    public Task<long> CreateAsync(
        string username,
        string passwordHash,
        string? displayName,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO sys_user (username, password_hash, display_name)
            VALUES (@username::text, @passwordHash::text, @displayName::text)
            RETURNING id
            """;

        return ExecuteAsync(
            db => db.ExecuteAsync<long>(
                sql,
                cancellationToken,
                Parameter("username", username),
                Parameter("passwordHash", passwordHash),
                Parameter("displayName", displayName)),
            cancellationToken);
    }

    public Task<bool> UpdateDisplayNameAsync(long id, string? displayName, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE sys_user
            SET display_name=@displayName::text, update_time=now()
            WHERE id=@id::bigint
            """;

        return ExecuteAsync(
            async db => await db.ExecuteAsync(
                sql,
                cancellationToken,
                Parameter("id", id),
                Parameter("displayName", displayName)) > 0,
            cancellationToken);
    }

    public Task<bool> TryDisableAsync(long id, CancellationToken cancellationToken)
    {
        // 单条语句同时保证：目标当前 active，且至少还有一个其他 active 账号。
        const string sql = """
            UPDATE sys_user
            SET status='disabled', update_time=now()
            WHERE id=@id::bigint
              AND status='active'
              AND (SELECT count(*) FROM sys_user WHERE status='active') > 1
            """;

        return ExecuteAsync(
            async db => await db.ExecuteAsync(sql, cancellationToken, Parameter("id", id)) > 0,
            cancellationToken);
    }

    public Task<bool> EnableAsync(long id, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE sys_user
            SET status='active', update_time=now()
            WHERE id=@id::bigint AND status='disabled'
            """;

        return ExecuteAsync(
            async db => await db.ExecuteAsync(sql, cancellationToken, Parameter("id", id)) > 0,
            cancellationToken);
    }

    public Task<bool> UpdatePasswordHashAsync(long id, string passwordHash, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE sys_user
            SET password_hash=@passwordHash::text, update_time=now()
            WHERE id=@id::bigint
            """;

        return ExecuteAsync(
            async db => await db.ExecuteAsync(
                sql,
                cancellationToken,
                Parameter("id", id),
                Parameter("passwordHash", passwordHash)) > 0,
            cancellationToken);
    }

    private static SysUserRecord ReadUser(DbDataReader reader)
    {
        return new SysUserRecord(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.GetString(2),
            GetNullableString(reader, 3),
            reader.GetString(4),
            GetDateTime(reader, 5),
            GetDateTime(reader, 6));
    }
}
