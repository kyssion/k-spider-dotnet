using KSpider.Admin.Common.Json;
using KSpider.Admin.Reconciliation;
using KSpider.Admin.Repository.Models;
using KSpider.Admin.Scheduler;
using KSpider.Admin.Tasks;
using KSpider.Admin.Tasks.Abstractions;
using KSpider.Admin.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Admin.Tests;

[TestClass]
public sealed class ReconciliationServiceTests
{
    private static readonly Guid DeadNodeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid AliveNodeId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [TestMethod]
    public async Task AliveRunningTaskIsNotRedispatched()
    {
        var harness = NewHarness(probeAlive: true);
        harness.Seed("running", failCount: 0);

        await harness.Service.ReconcileStaleTasksAsync(CancellationToken.None);

        Assert.AreEqual(0, harness.Dispatcher.Dispatches.Count);
        Assert.AreEqual("running", harness.Repository.Tasks.Single().Status);
    }

    [TestMethod]
    public async Task DeadRunningTaskIsRedispatched()
    {
        var harness = NewHarness(probeAlive: false);
        harness.Seed("running", failCount: 0);

        await harness.Service.ReconcileStaleTasksAsync(CancellationToken.None);

        Assert.AreEqual(1, harness.Dispatcher.Dispatches.Count);
        Assert.AreEqual("pushed", harness.Repository.Tasks.Single().Status);
        var extra = AdminJson.Deserialize<TaskExtraView>(harness.Repository.Tasks.Single().ExtraJson);
        Assert.AreEqual(1, extra!.FailCount);
    }

    [TestMethod]
    public async Task FailLimitMovesTaskToAbandoned()
    {
        var harness = NewHarness(probeAlive: false);
        harness.Seed("run_failed", failCount: 3);

        await harness.Service.ReconcileStaleTasksAsync(CancellationToken.None);

        Assert.AreEqual(0, harness.Dispatcher.Dispatches.Count);
        Assert.AreEqual("abandoned", harness.Repository.Tasks.Single().Status);
    }

    private static Harness NewHarness(bool probeAlive)
    {
        var repository = new FakeTaskRepository();
        var dispatcher = new FakeNodeDispatcher();
        var observer = new RecordingTerminalObserver();
        var adminTasks = new AdminTaskService(
            repository,
            dispatcher,
            [observer],
            NullLogger<AdminTaskService>.Instance);
        var probe = new FakeProbeService { Alive = probeAlive };
        var directory = new FakeNodeDirectory();
        directory.Add(new NodeSnapshot(AliveNodeId, NodeTypes.Crawler, 0, DateTimeOffset.UtcNow));
        var guard = new ChannelRoundGuard(NullLogger<ChannelRoundGuard>.Instance);
        var options = Options.Create(new ReconciliationOptions
        {
            InactivityTimeoutSeconds = 1,
            ProbeTimeoutSeconds = 1,
            ProbeAttempts = 1,
            ProbeWindowSeconds = 1,
            MaxFailCount = 3,
            ScanBatchSize = 10,
        });
        var service = new ReconciliationService(
            repository,
            adminTasks,
            probe,
            directory,
            dispatcher,
            guard,
            new NodeSelector(),
            options,
            TimeProvider.System,
            NullLogger<ReconciliationService>.Instance);
        return new Harness(service, repository, dispatcher);
    }

    private sealed class Harness
    {
        public Harness(
            ReconciliationService service,
            FakeTaskRepository repository,
            FakeNodeDispatcher dispatcher)
        {
            Service = service;
            Repository = repository;
            Dispatcher = dispatcher;
        }

        public ReconciliationService Service { get; }

        public FakeTaskRepository Repository { get; }

        public FakeNodeDispatcher Dispatcher { get; }

        public void Seed(string status, int failCount)
        {
            Repository.Seed(new SysTaskRecord(
                1,
                DeadNodeId,
                TaskTypes.ChannelRound,
                AdminJson.Serialize(new TaskDetail(7, RoundTypes.List, 100)),
                status,
                AdminJson.Serialize(new { dispatch_token = "token-1", fail_count = failCount }),
                DateTime.UtcNow.AddMinutes(-10),
                DateTime.UtcNow.AddMinutes(-10)));
        }
    }
}
