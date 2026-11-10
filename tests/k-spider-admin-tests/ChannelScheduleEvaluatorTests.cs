using KSpider.Admin.Scheduler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Admin.Tests;

[TestClass]
public sealed class ChannelScheduleEvaluatorTests
{
    private readonly ChannelScheduleEvaluator _evaluator = new();

    [TestMethod]
    public void DisabledScheduleIsNeverDue()
    {
        Assert.IsFalse(_evaluator.IsDue(
            new ChannelSchedule(false, 60),
            DateTimeOffset.UtcNow,
            null));
    }

    [TestMethod]
    public void FirstRunIsDueImmediately()
    {
        Assert.IsTrue(_evaluator.IsDue(
            new ChannelSchedule(true, 60),
            DateTimeOffset.UtcNow,
            null));
    }

    [TestMethod]
    public void WaitsForInterval()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.IsFalse(_evaluator.IsDue(
            new ChannelSchedule(true, 60),
            now,
            now.AddSeconds(-59)));
    }

    [TestMethod]
    public void DueAfterInterval()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.IsTrue(_evaluator.IsDue(
            new ChannelSchedule(true, 60),
            now,
            now.AddSeconds(-60)));
    }
}
