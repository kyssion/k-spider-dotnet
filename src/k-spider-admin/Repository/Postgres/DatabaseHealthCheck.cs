using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KSpider.Admin.Repository.Postgres;

/// <summary>
///     数据库可达性健康检查。
/// </summary>
/// <remarks>
///     只执行 SELECT 1，不读写业务表；异常被转换为 Unhealthy 并保留原始异常供日志排查。
/// </remarks>
/// <param name="connectionFactory">数据库连接工厂。</param>
public sealed class DatabaseHealthCheck(IAdminDbConnectionFactory connectionFactory) : IHealthCheck
{
    /// <summary>
    ///     执行健康检查。
    /// </summary>
    /// <param name="context">健康检查上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = connectionFactory.Create();
            var value = await connection.ExecuteAsync<long>("SELECT 1", cancellationToken);
            return value == 1
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("数据库返回了非预期结果");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("数据库不可达", exception);
        }
    }
}
