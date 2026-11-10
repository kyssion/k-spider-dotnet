using KSpider.Admin.Api.Contracts;
using KSpider.Admin.Repository.Abstractions;
using KSpider.Admin.Repository.Models;

namespace KSpider.Admin.Api.Queries;

/// <summary>
///     渠道与分类的只读查询服务。
/// </summary>
/// <remarks>
///     负责把仓储记录映射为 API 视图，不在这里做权限、分页越界修正或业务状态变更；
///     这些规则分别在鉴权中间件与仓储层。
/// </remarks>
/// <param name="channels">渠道仓储。</param>
public sealed class ChannelQueryService(IChannelRepository channels)
{
    /// <summary>
    ///     分页查询渠道。
    /// </summary>
    /// <param name="query">筛选与分页条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<PageResponse<ChannelView>> QueryAsync(
        ChannelQuery query,
        CancellationToken cancellationToken)
    {
        var page = await channels.QueryAsync(query, cancellationToken);
        return new PageResponse<ChannelView>(
            page.Items.Select(Map).ToList(),
            page.Page,
            page.PageSize,
            page.Total);
    }

    /// <summary>
    ///     读取单个渠道。
    /// </summary>
    /// <param name="id">渠道主键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>渠道视图；不存在时返回 null。</returns>
    public async Task<ChannelView?> GetAsync(long id, CancellationToken cancellationToken)
    {
        var channel = await channels.GetAsync(id, cancellationToken);
        return channel is null ? null : Map(channel);
    }

    /// <summary>
    ///     读取渠道分类树。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<IReadOnlyList<ChannelCategoryView>> ListCategoriesAsync(
        CancellationToken cancellationToken)
    {
        var categories = await channels.ListCategoriesAsync(cancellationToken);
        return categories
            .Select(category => new ChannelCategoryView(
                category.Id,
                category.ParentId,
                category.Name,
                category.Path,
                category.SortNo,
                category.Status))
            .ToList();
    }

    private static ChannelView Map(ChannelRecord channel)
    {
        return new ChannelView(
            channel.Id,
            channel.CategoryId,
            channel.ChannelName,
            channel.SourceAdapter,
            channel.DetailAdapter,
            channel.WriteMode,
            channel.RetentionDays,
            channel.Status,
            channel.ScheduleJson,
            channel.ExtraJson,
            channel.CreateTime,
            channel.UpdateTime);
    }
}
