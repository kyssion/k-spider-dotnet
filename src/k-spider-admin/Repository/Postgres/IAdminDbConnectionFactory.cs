using LinqToDB.Data;

namespace KSpider.Admin.Repository.Postgres;

/// <summary>
///     创建 linq2db PostgreSQL 连接；每个仓储操作独立创建、独立释放。
/// </summary>
/// <summary>
///     创建 linq2db PostgreSQL 连接；每个仓储操作独立创建、独立释放。
/// </summary>
public interface IAdminDbConnectionFactory
{
    /// <summary>
    ///     创建一个新连接；调用方负责释放。
    /// </summary>
    /// <returns>linq2db 数据库连接。</returns>
    DataConnection Create();
}
