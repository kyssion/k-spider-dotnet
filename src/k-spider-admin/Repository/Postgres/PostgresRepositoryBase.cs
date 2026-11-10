using System.Data.Common;
using KSpider.Admin.Repository.Exceptions;
using LinqToDB.Data;

namespace KSpider.Admin.Repository.Postgres;

/// <summary>
///     PostgreSQL 仓储公共连接与异常处理。
/// </summary>
/// <remarks>
///     每次操作创建独立连接并在结束时释放；取消异常原样透传，其余数据库异常统一包成
///     <see cref="KDbException" />，让上层能区分数据库故障与业务失败。
/// </remarks>
internal abstract class PostgresRepositoryBase
{
    private readonly IAdminDbConnectionFactory _connectionFactory;

    /// <summary>
    ///     注入连接工厂。
    /// </summary>
    /// <param name="connectionFactory">连接工厂。</param>
    protected PostgresRepositoryBase(IAdminDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    ///     在独立连接上执行数据库操作。
    /// </summary>
    /// <typeparam name="T">返回值类型。</typeparam>
    /// <param name="action">数据库操作。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    protected async Task<T> ExecuteAsync<T>(
        Func<DataConnection, Task<T>> action,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = _connectionFactory.Create();
            return await action(connection);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (KDbException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new KDbException("数据库操作失败", exception);
        }
    }

    /// <summary>
    ///     执行不返回值的数据库操作。
    /// </summary>
    /// <param name="action">数据库操作。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    protected async Task<int> ExecuteAsync(
        Func<DataConnection, Task<int>> action,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync<int>(action, cancellationToken);
    }

    /// <summary>
    ///     创建命名参数；SQL 中统一使用 <c>@name</c> 引用。
    /// </summary>
    protected static DataParameter Parameter(string name, object? value)
    {
        return new DataParameter(name, value);
    }

    /// <summary>
    ///     读取可空文本列。
    /// </summary>
    protected static string? GetNullableString(DbDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    /// <summary>
    ///     读取可空 int 列。
    /// </summary>
    protected static int? GetNullableInt32(DbDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    /// <summary>
    ///     读取 timestamptz 列。
    /// </summary>
    protected static DateTime GetDateTime(DbDataReader reader, int ordinal)
    {
        return reader.GetFieldValue<DateTime>(ordinal);
    }
}
