using KSpider.Collection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Lib;

[TestClass]
public class ListToolsTest
{
    [TestMethod]
    public void PartitionEvenSplit()
    {
        var chunks = ListTools.Partition(new List<int> { 1, 2, 3, 4 }, 2);
        Assert.AreEqual(2, chunks.Count);
        CollectionAssert.AreEqual(new List<int> { 1, 2 }, chunks[0]);
        CollectionAssert.AreEqual(new List<int> { 3, 4 }, chunks[1]);
    }

    [TestMethod]
    public void PartitionRemainderChunk()
    {
        var chunks = ListTools.Partition(new List<int> { 1, 2, 3, 4, 5 }, 2);
        Assert.AreEqual(3, chunks.Count);
        CollectionAssert.AreEqual(new List<int> { 5 }, chunks[2]);
    }

    [TestMethod]
    public void PartitionChunkSizeLargerThanList()
    {
        var chunks = ListTools.Partition(new List<int> { 1, 2 }, 10);
        Assert.AreEqual(1, chunks.Count);
        CollectionAssert.AreEqual(new List<int> { 1, 2 }, chunks[0]);
    }

    [TestMethod]
    public void PartitionEmptyList()
    {
        Assert.AreEqual(0, ListTools.Partition(new List<int>(), 3).Count);
    }
}
