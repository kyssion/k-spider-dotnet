using KSpider.Admin.Repository.Models;

namespace KSpider.Admin.Repository.Abstractions;

/// <summary>
///     原始数据只读查询。
/// </summary>
/// <remarks>
///     列表查询不读取 raw 列，只取 `octet_length(raw)`；详情查询才返回原始载荷。
/// </remarks>
public interface IRawDataRepository
{
    /// <summary>
    ///     分页查询 source。
    /// </summary>
    /// <param name="query">筛选与分页条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<PageResult<RawDataSourceRecord>> QuerySourcesAsync(
        RawDataSourceQuery query,
        CancellationToken cancellationToken);

    /// <summary>
    ///     读取 source 详情，包含 raw。
    /// </summary>
    /// <param name="id">source 主键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<RawDataSourceRecord?> GetSourceAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    ///     判断 source 是否存在，用于 detail 列表的 404 判断。
    /// </summary>
    /// <param name="id">source 主键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<bool> SourceExistsAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    ///     分页查询指定 source 下的 detail。
    /// </summary>
    /// <param name="sourceId">source 主键。</param>
    /// <param name="page">页码。</param>
    /// <param name="pageSize">每页数量。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<PageResult<RawDataDetailRecord>> QueryDetailsAsync(
        long sourceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    ///     读取 detail 详情，包含 raw。
    /// </summary>
    /// <param name="id">detail 主键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<RawDataDetailRecord?> GetDetailAsync(long id, CancellationToken cancellationToken);
}
