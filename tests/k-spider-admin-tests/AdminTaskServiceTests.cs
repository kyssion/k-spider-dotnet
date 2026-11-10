using KSpider.Admin.Common.Json;
using KSpider.Admin.Repository.Models;
using KSpider.Admin.Tasks;
using KSpider.Admin.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Admin.Tests;

[TestClass]
public sealed class AdminTaskServiceTests
{
    private static readonly Guid NodeId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [TestMethod]
    public async Task CreateForDispatchStoresPushingWithToken()
    {
        var (service, repository, _, _) = NewService();
        var task = await service.CreateForDispatchAsync(NewPlan(), NodeId, CancellationToken.None);

        Assert.AreEqual("pushing", task.Status);
        Assert.AreEqual(NodeId, task.NodeId);
        var extra = AdminJson.Deserialize<TaskExtraView>(task.ExtraJson);
        Assert.IsNotNull(extra);
        Assert.IsFalse(string.IsNullOrWhiteSpace(extra.DispatchToken));
        Assert.AreEqual(0, extra.FailCount);
        Assert.AreEqual(1, repository.Tasks.Count);
    }

    [TestMethod]
    public async Task MarkPushedRequiresCurrentToken()
    {
        var (service, _, _, _) = NewService();
        var task = await service.CreateForDispatchAsync(NewPlan(), NodeId, CancellationToken.None);
        var token = TokenOf(task);

        var applied = await service.MarkPushedAsync(task.Id, token, CancellationToken.None);
        Assert.IsTrue(applied.Applied);

        var stale = await service.MarkPushedAsync(
            task.Id,
            DispatchToken.From("stale-token"),
            CancellationToken.None);
        Assert.AreEqual(TaskTransitionOutcome.Conflict, stale.Outcome);
    }

    [TestMethod]
    public async Task CancelUpdatesStateNotifiesObserverAndSendsFastPath()
    {
        var (service, _, dispatcher, observer) = NewService();
        var task = await service.CreateForDispatchAsync(NewPlan(), NodeId, CancellationToken.None);

        var result = await service.TryCancelAsync(task.Id, "manual", CancellationToken.None);

        Assert.IsTrue(result.Applied);
        Assert.AreEqual("cancelled", (await service.GetAsync(task.Id, CancellationToken.None))!.Status);
        Assert.AreEqual(1, dispatcher.Cancels.Count);
        Assert.AreEqual(1, observer.Notifications.Count);
        Assert.AreEqual(task.Id, observer.Notifications[0].TaskId);
        Assert.AreEqual(NewPlan().ChannelId, observer.Notifications[0].ChannelId);
    }

    [TestMethod]
    public async Task RedispatchStopsAtFailLimit()
    {
        var (service, repository, _, _) = NewService();
        var task = await SeedTaskAsync(repository, "run_failed", failCount: 3);

        var result = await service.TryRedispatchAsync(
            task.Id,
            NewPlan(),
            NodeId,
            3,
            CancellationToken.None);

        Assert.AreEqual(TaskTransitionOutcome.FailLimitReached, result.Outcome);
        Assert.AreEqual("run_failed", repository.Tasks.Single().Status);
    }

    [TestMethod]
    public async Task ObserveTerminalNodeResultNotifiesObserverWithoutWritingStatus()
    {
        var (service, repository, _, observer) = NewService();
        var task = await SeedTaskAsync(repository, "success", failCount: 0);

        var result = await service.ObserveNodeResultAsync(
            task.Id,
            TokenOf(task),
            AdminTaskStatus.Success,
            CancellationToken.None);

        Assert.IsTrue(result.Applied);
        Assert.AreEqual("success", repository.Tasks.Single().Status);
        Assert.AreEqual(1, observer.Notifications.Count);
    }

    [TestMethod]
    public async Task ObserveNonTerminalNodeResultIsConflict()
    {
        var (service, repository, _, observer) = NewService();
        var task = await SeedTaskAsync(repository, "running", failCount: 0);

        var result = await service.ObserveNodeResultAsync(
            task.Id,
            TokenOf(task),
            AdminTaskStatus.Success,
            CancellationToken.None);

        Assert.AreEqual(TaskTransitionOutcome.Conflict, result.Outcome);
        Assert.AreEqual(0, observer.Notifications.Count);
    }

    private static TaskPlan NewPlan()
    {
        return new TaskPlan(
            7,
            TaskTypes.ChannelRound,
            new TaskDetail(7, RoundTypes.List, 100));
    }

    private static DispatchToken TokenOf(SysTaskRecord task)
    {
        var extra = AdminJson.Deserialize<TaskExtraView>(task.ExtraJson);
        Assert.IsNotNull(extra?.DispatchToken);
        return DispatchToken.From(extra.DispatchToken);
    }

    private static async Task<SysTaskRecord> SeedTaskAsync(
        FakeTaskRepository repository,
        string status,
        int failCount)
    {
        var record = new SysTaskRecord(
            100,
            NodeId,
            TaskTypes.ChannelRound,
            AdminJson.Serialize(new TaskDetail(7, RoundTypes.List, 100)),
            status,
            AdminJson.Serialize(new { dispatch_token = "token-1", fail_count = failCount }),
            DateTime.UtcNow.AddMinutes(-5),
            DateTime.UtcNow.AddMinutes(-5));
        repository.Seed(record);
        await Task.CompletedTask;
        return record;
    }

    private static (AdminTaskService Service, FakeTaskRepository Repository, FakeNodeDispatcher Dispatcher,
        RecordingTerminalObserver Observer) NewService()
    {
        var repository = new FakeTaskRepository();
        var dispatcher = new FakeNodeDispatcher();
        var observer = new RecordingTerminalObserver();
        var service = new AdminTaskService(
            repository,
            dispatcher,
            [observer],
            NullLogger<AdminTaskService>.Instance);
        return (service, repository, dispatcher, observer);
    }
}
