using KSpider.Admin.Repository.Models;

namespace KSpider.Admin.Repository.Abstractions;

/// <summary>
///     channel / channel_category 数据访问边界。
/// </summary>
/// <remarks>
///     调度只读取 active 渠道；渠道启停命令与查询走同一仓储，保证状态字段只有一个写入口。
/// </remarks>
public interface IChannelRepository
{
    /// <summary>
    ///     读取所有 active 渠道，供调度循环判断到期。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<IReadOnlyList<ChannelRecord>> ListActiveForSchedulingAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     按主键读取渠道。
    /// </summary>
    /// <param name="id">渠道主键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<ChannelRecord?> GetAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    ///     分页查询渠道。
    /// </summary>
    /// <param name="query">分类、状态与分页条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<PageResult<ChannelRecord>> QueryAsync(ChannelQuery query, CancellationToken cancellationToken);

    /// <summary>
    ///     读取渠道分类树。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<IReadOnlyList<ChannelCategoryRecord>> ListCategoriesAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     更新渠道状态（active / paused / retired）。
    /// </summary>
    /// <param name="id">渠道主键。</param>
    /// <param name="status">目标状态；调用方保证取值合法。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>目标行存在并更新成功返回 true。</returns>
    Task<bool> UpdateStatusAsync(long id, string status, CancellationToken cancellationToken);
}
