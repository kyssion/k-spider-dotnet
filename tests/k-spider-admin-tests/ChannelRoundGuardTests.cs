using KSpider.Admin.Common.Json;
using KSpider.Admin.Repository.Models;
using KSpider.Admin.Scheduler;
using KSpider.Admin.Tasks;
using KSpider.Admin.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Admin.Tests;

[TestClass]
public sealed class ChannelRoundGuardTests
{
    [TestMethod]
    public void AcquireAndReleaseRound()
    {
        var guard = NewGuard();
        Assert.IsTrue(guard.TryAcquire(7, 42));
        Assert.IsTrue(guard.IsHeld(7));
        guard.Release(7, 42);
        Assert.IsFalse(guard.IsHeld(7));
    }

    [TestMethod]
    public void EarlierTaskCannotReleaseNewerRound()
    {
        var guard = NewGuard();
        Assert.IsTrue(guard.TryAcquire(7, 42));
        Assert.IsFalse(guard.TryAcquire(7, 43));
        guard.Release(7, 43);
        Assert.IsTrue(guard.IsHeld(7));
    }

    [TestMethod]
    public async Task RestoresNonTerminalTasksAtStartup()
    {
        var repository = new FakeTaskRepository();
        var detail = new TaskDetail(9, RoundTypes.List, 100);
        await repository.InsertAsync(
            new SysTaskInsert(
                Guid.NewGuid(),
                TaskTypes.ChannelRound,
                AdminJson.Serialize(detail),
                AdminTaskStatus.Running.ToDbValue(),
                "{}"),
            CancellationToken.None);

        var guard = NewGuard();
        await guard.RestoreFromTasksAsync(repository, CancellationToken.None);

        Assert.IsTrue(guard.IsHeld(9));
        Assert.IsTrue(guard.TryGetActiveTaskId(9, out var taskId));
        Assert.AreEqual(1, taskId);
    }

    [TestMethod]
    public async Task TerminalObserverReleasesRound()
    {
        var guard = NewGuard();
        Assert.IsTrue(guard.TryAcquire(9, 1));
        await guard.OnTaskTerminalAsync(1, 9, CancellationToken.None);
        Assert.IsFalse(guard.IsHeld(9));
    }

    private static ChannelRoundGuard NewGuard()
    {
        return new ChannelRoundGuard(NullLogger<ChannelRoundGuard>.Instance);
    }
}
