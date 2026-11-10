using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.Options;

namespace KSpider.Admin.Repository.Postgres;

/// <summary>
///     按配置创建 linq2db 连接；每个仓储操作创建一个独立连接。
/// </summary>
/// <remarks>
///     DataOptions 只构建一次并可安全复用；DataConnection 不跨操作共享，避免连接状态、
///     事务或命令在并发请求之间串用。
/// </remarks>
internal sealed class PostgresConnectionFactory : IAdminDbConnectionFactory
{
    private readonly DataOptions _options;

    /// <summary>
    ///     校验连接串并构建 PostgreSQL provider 配置。
    /// </summary>
    /// <param name="options">数据库配置。</param>
    /// <exception cref="InvalidOperationException">连接串为空。</exception>
    public PostgresConnectionFactory(IOptions<DatabaseOptions> options)
    {
        var connectionString = options.Value.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database:ConnectionString 未配置");
        }

        _options = new DataOptions().UsePostgreSQL(connectionString);
    }

    /// <summary>
    ///     创建一个新的数据库连接。
    /// </summary>
    public DataConnection Create()
    {
        return new DataConnection(_options);
    }
}
