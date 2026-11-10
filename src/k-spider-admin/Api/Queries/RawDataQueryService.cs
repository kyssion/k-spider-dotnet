using KSpider.Admin.Api.Contracts;
using KSpider.Admin.Repository.Abstractions;
using KSpider.Admin.Repository.Models;

namespace KSpider.Admin.Api.Queries;

/// <summary>
///     原始数据只读响应映射；raw 只在详情中编码为 Base64。
/// </summary>
/// <param name="rawData">原始数据仓储。</param>
public sealed class RawDataQueryService(IRawDataRepository rawData)
{
    /// <summary>
    ///     分页查询 source。
    /// </summary>
    /// <param name="query">筛选与分页条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<PageResponse<RawDataSourceListItem>> QuerySourcesAsync(
        RawDataSourceQuery query,
        CancellationToken cancellationToken)
    {
        var page = await rawData.QuerySourcesAsync(query, cancellationToken);
        return new PageResponse<RawDataSourceListItem>(
            page.Items.Select(MapSourceList).ToList(),
            page.Page,
            page.PageSize,
            page.Total);
    }

    /// <summary>
    ///     读取 source 详情；不存在返回 null。
    /// </summary>
    /// <param name="id">source 主键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<RawDataSourceDetailView?> GetSourceAsync(
        long id,
        CancellationToken cancellationToken)
    {
        var source = await rawData.GetSourceAsync(id, cancellationToken);
        return source is null ? null : MapSourceDetail(source);
    }

    /// <summary>
    ///     分页查询 source 下的 detail；source 不存在返回 null。
    /// </summary>
    /// <param name="sourceId">source 主键。</param>
    /// <param name="page">页码。</param>
    /// <param name="pageSize">每页数量。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<PageResponse<RawDataDetailListItem>?> QueryDetailsAsync(
        long sourceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!await rawData.SourceExistsAsync(sourceId, cancellationToken))
        {
            return null;
        }

        var result = await rawData.QueryDetailsAsync(sourceId, page, pageSize, cancellationToken);
        return new PageResponse<RawDataDetailListItem>(
            result.Items.Select(MapDetailList).ToList(),
            result.Page,
            result.PageSize,
            result.Total);
    }

    /// <summary>
    ///     读取 detail 详情；不存在返回 null。
    /// </summary>
    /// <param name="id">detail 主键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<RawDataDetailDetailView?> GetDetailAsync(
        long id,
        CancellationToken cancellationToken)
    {
        var detail = await rawData.GetDetailAsync(id, cancellationToken);
        return detail is null ? null : MapDetailDetail(detail);
    }

    private static RawDataSourceListItem MapSourceList(RawDataSourceRecord record)
    {
        return new RawDataSourceListItem(
            record.Id,
            record.ChannelId,
            record.ChannelName,
            record.Adapter,
            record.RecordKey,
            record.DataStatus,
            record.ExecStatus,
            record.RawSize,
            record.CreateTime,
            record.UpdateTime);
    }

    private static RawDataSourceDetailView MapSourceDetail(RawDataSourceRecord record)
    {
        return new RawDataSourceDetailView(
            record.Id,
            record.ChannelId,
            record.ChannelName,
            record.Adapter,
            record.RecordKey,
            record.DataStatus,
            record.ExecStatus,
            record.RawSize,
            ToBase64(record.Raw),
            "base64",
            record.CreateTime,
            record.UpdateTime);
    }

    private static RawDataDetailListItem MapDetailList(RawDataDetailRecord record)
    {
        return new RawDataDetailListItem(
            record.Id,
            record.SourceId,
            record.FetchUrl,
            record.Adapter,
            record.RecordKey,
            record.DataStatus,
            record.ExecStatus,
            record.RawSize,
            record.CreateTime,
            record.UpdateTime);
    }

    private static RawDataDetailDetailView MapDetailDetail(RawDataDetailRecord record)
    {
        return new RawDataDetailDetailView(
            record.Id,
            record.SourceId,
            record.FetchUrl,
            record.Adapter,
            record.RecordKey,
            record.DataStatus,
            record.ExecStatus,
            record.RawSize,
            ToBase64(record.Raw),
            "base64",
            record.CreateTime,
            record.UpdateTime);
    }

    private static string ToBase64(byte[]? raw)
    {
        return raw is null ? string.Empty : Convert.ToBase64String(raw);
    }
}
