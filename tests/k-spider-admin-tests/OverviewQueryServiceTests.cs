using KSpider.Admin.Api.Queries;
using KSpider.Admin.Repository.Models;
using KSpider.Admin.Tasks;
using KSpider.Admin.Tests.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Admin.Tests;

[TestClass]
public sealed class OverviewQueryServiceTests
{
    [TestMethod]
    public async Task FillsMissingTaskStatusesWithZero()
    {
        var repository = new FakeOverviewRepository
        {
            Snapshot = new OverviewSnapshot(
                new ChannelStatusCounts(4, 2, 1, 1),
                5,
                new Dictionary<string, long> { ["running"] = 2, ["success"] = 3 },
                new NodeStatusCounts(3, 2, 1),
                []),
        };
        var service = new OverviewQueryService(repository, TimeProvider.System);

        var response = await service.GetAsync(CancellationToken.None);

        Assert.AreEqual(4, response.Channels.Total);
        Assert.AreEqual(2, response.Channels.Active);
        Assert.AreEqual(5, response.Tasks.Total);
        Assert.AreEqual(2, response.Tasks.ByStatus[AdminTaskStatus.Running.ToDbValue()]);
        Assert.AreEqual(0, response.Tasks.ByStatus[AdminTaskStatus.PushFailed.ToDbValue()]);
        Assert.AreEqual(3, response.Nodes.Total);
        Assert.AreEqual(DateTimeKind.Utc, response.GeneratedAt.Kind);
    }

    [TestMethod]
    public async Task MapsRecentFailures()
    {
        var nodeId = Guid.NewGuid();
        var repository = new FakeOverviewRepository
        {
            Snapshot = new OverviewSnapshot(
                new ChannelStatusCounts(0, 0, 0, 0),
                1,
                new Dictionary<string, long>(),
                new NodeStatusCounts(0, 0, 0),
                [new RecentTaskFailure(7, 3, nodeId, "run_failed", 2, "boom", DateTime.UtcNow)]),
        };
        var service = new OverviewQueryService(repository, TimeProvider.System);

        var response = await service.GetAsync(CancellationToken.None);

        var failure = response.RecentFailures.Single();
        Assert.AreEqual(7, failure.TaskId);
        Assert.AreEqual(3, failure.ChannelId);
        Assert.AreEqual(nodeId, failure.NodeId);
        Assert.AreEqual("run_failed", failure.Status);
        Assert.AreEqual(2, failure.FailCount);
        Assert.AreEqual("boom", failure.LastError);
    }
}
