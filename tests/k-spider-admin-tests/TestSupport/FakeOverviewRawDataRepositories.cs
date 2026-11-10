using KSpider.Admin.Repository.Abstractions;
using KSpider.Admin.Repository.Models;

namespace KSpider.Admin.Tests.TestSupport;

internal sealed class FakeOverviewRepository : IOverviewRepository
{
    public OverviewSnapshot Snapshot { get; set; } = new(
        new ChannelStatusCounts(0, 0, 0, 0),
        0,
        new Dictionary<string, long>(),
        new NodeStatusCounts(0, 0, 0),
        []);

    public Task<OverviewSnapshot> GetAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Snapshot);
    }
}

internal sealed class FakeRawDataRepository : IRawDataRepository
{
    public List<RawDataSourceRecord> Sources { get; } = [];

    public List<RawDataDetailRecord> Details { get; } = [];

    public Task<PageResult<RawDataSourceRecord>> QuerySourcesAsync(
        RawDataSourceQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var items = Sources
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return Task.FromResult(new PageResult<RawDataSourceRecord>(items, page, pageSize, Sources.Count));
    }

    public Task<RawDataSourceRecord?> GetSourceAsync(long id, CancellationToken cancellationToken)
    {
        return Task.FromResult(Sources.FirstOrDefault(source => source.Id == id));
    }

    public Task<bool> SourceExistsAsync(long id, CancellationToken cancellationToken)
    {
        return Task.FromResult(Sources.Any(source => source.Id == id));
    }

    public Task<PageResult<RawDataDetailRecord>> QueryDetailsAsync(
        long sourceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var sourceDetails = Details.Where(detail => detail.SourceId == sourceId).ToList();
        var items = sourceDetails
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToList();
        return Task.FromResult(new PageResult<RawDataDetailRecord>(
            items,
            normalizedPage,
            normalizedPageSize,
            sourceDetails.Count));
    }

    public Task<RawDataDetailRecord?> GetDetailAsync(long id, CancellationToken cancellationToken)
    {
        return Task.FromResult(Details.FirstOrDefault(detail => detail.Id == id));
    }
}
