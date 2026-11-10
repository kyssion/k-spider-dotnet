using KSpider.Admin.Api.Queries;
using KSpider.Admin.Repository.Models;
using KSpider.Admin.Tests.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Admin.Tests;

[TestClass]
public sealed class RawDataQueryServiceTests
{
    [TestMethod]
    public async Task ListDoesNotExposeRaw()
    {
        var repository = new FakeRawDataRepository();
        repository.Sources.Add(Source(1, [1, 2, 3]));
        var service = new RawDataQueryService(repository);

        var page = await service.QuerySourcesAsync(new RawDataSourceQuery(1, 20), CancellationToken.None);

        Assert.AreEqual(1, page.Total);
        Assert.AreEqual(3, page.Items[0].RawSize);
    }

    [TestMethod]
    public async Task SourceDetailEncodesRawAsBase64()
    {
        var repository = new FakeRawDataRepository();
        repository.Sources.Add(Source(1, [1, 2, 3]));
        var service = new RawDataQueryService(repository);

        var detail = await service.GetSourceAsync(1, CancellationToken.None);

        Assert.IsNotNull(detail);
        Assert.AreEqual("AQID", detail.RawBase64);
        Assert.AreEqual("base64", detail.RawEncoding);
    }

    [TestMethod]
    public async Task MissingSourceDetailsReturnsNull()
    {
        var service = new RawDataQueryService(new FakeRawDataRepository());

        var page = await service.QueryDetailsAsync(99, 1, 20, CancellationToken.None);

        Assert.IsNull(page);
    }

    [TestMethod]
    public async Task DetailListAndDetailMapRaw()
    {
        var repository = new FakeRawDataRepository();
        repository.Sources.Add(Source(1, []));
        repository.Details.Add(new RawDataDetailRecord(
            10,
            1,
            "https://example.test/1",
            "detail",
            "key",
            "normal",
            "success",
            2,
            [4, 5],
            DateTime.UtcNow,
            DateTime.UtcNow));
        var service = new RawDataQueryService(repository);

        var list = await service.QueryDetailsAsync(1, 1, 20, CancellationToken.None);
        Assert.IsNotNull(list);
        Assert.AreEqual(2, list.Items[0].RawSize);

        var detail = await service.GetDetailAsync(10, CancellationToken.None);
        Assert.IsNotNull(detail);
        Assert.AreEqual("BAU=", detail.RawBase64);
    }

    private static RawDataSourceRecord Source(long id, byte[] raw)
    {
        return new RawDataSourceRecord(
            id,
            5,
            "测试渠道",
            "adapter",
            $"key-{id}",
            "normal",
            "success",
            raw.Length,
            raw,
            DateTime.UtcNow,
            DateTime.UtcNow);
    }
}
