using KSpider.Admin.Scheduler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Admin.Tests;

[TestClass]
public sealed class ChannelScheduleTests
{
    [TestMethod]
    public void ParsesEnabledInterval()
    {
        var schedule = ChannelSchedule.Parse("""{"enabled":true,"interval_seconds":60}""");
        Assert.IsTrue(schedule.Enabled);
        Assert.AreEqual(60, schedule.IntervalSeconds);
    }

    [TestMethod]
    public void EmptyObjectIsDisabled()
    {
        var schedule = ChannelSchedule.Parse("{}");
        Assert.IsFalse(schedule.Enabled);
        Assert.AreEqual(0, schedule.IntervalSeconds);
    }

    [TestMethod]
    public void UnknownKeyFailsFast()
    {
        Assert.ThrowsExactly<FormatException>(
            () => ChannelSchedule.Parse("""{"enabled":true,"interval_seconds":60,"cron":"* * * * *"}"""));
    }

    [TestMethod]
    public void EnabledWithoutIntervalFailsFast()
    {
        Assert.ThrowsExactly<FormatException>(
            () => ChannelSchedule.Parse("""{"enabled":true}"""));
    }

    [TestMethod]
    public void NonPositiveIntervalFailsFast()
    {
        Assert.ThrowsExactly<FormatException>(
            () => ChannelSchedule.Parse("""{"enabled":true,"interval_seconds":0}"""));
    }
}
