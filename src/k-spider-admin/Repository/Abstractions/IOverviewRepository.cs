using KSpider.Admin.Repository.Models;

namespace KSpider.Admin.Repository.Abstractions;

/// <summary>
///     总览统计查询。
/// </summary>
public interface IOverviewRepository
{
    /// <summary>
    ///     读取一次总览快照。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<OverviewSnapshot> GetAsync(CancellationToken cancellationToken);
}
