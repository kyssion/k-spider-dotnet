using LinqToDB.Data;

namespace KSpider.Admin.Repository.Postgres;

/// <summary>
///     创建 linq2db PostgreSQL 连接；每个仓储操作独立创建、独立释放。
/// </summary>
public interface IAdminDbConnectionFactory
{
    DataConnection Create();
}
