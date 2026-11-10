using KSpider.Admin.Scheduler;
using KSpider.Admin.Tasks;
using KSpider.Admin.Tasks.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Admin.Tests;

[TestClass]
public sealed class NodeSelectorTests
{
    [TestMethod]
    public void SelectsCrawlerWithLowestRunningTasks()
    {
        var busy = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var idle = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var nodes = new[]
        {
            new NodeSnapshot(busy, NodeTypes.Crawler, 5, DateTimeOffset.UtcNow),
            new NodeSnapshot(idle, NodeTypes.Crawler, 1, DateTimeOffset.UtcNow),
            new NodeSnapshot(Guid.NewGuid(), NodeTypes.Process, 0, DateTimeOffset.UtcNow),
        };

        var selected = new NodeSelector().Select(nodes);

        Assert.IsNotNull(selected);
        Assert.AreEqual(idle, selected.NodeId);
    }

    [TestMethod]
    public void ReturnsNullWhenNoCrawlerExists()
    {
        var nodes = new[]
        {
            new NodeSnapshot(Guid.NewGuid(), NodeTypes.Process, 0, DateTimeOffset.UtcNow),
        };

        Assert.IsNull(new NodeSelector().Select(nodes));
    }
}
