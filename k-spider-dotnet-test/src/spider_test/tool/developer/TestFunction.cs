namespace k_spider_dotnet_test.spider_test.tool.developer;

[TestClass]
public class TestFunction
{
    [TestMethod]
    public void TestTask()
    {
        var tasksList = new List<Task>();
        tasksList.Add(Task.Run(() =>
        {
            Thread.Sleep(1000);
        }));
        tasksList.Add(Task.Run(() =>
        {
            Thread.Sleep(1000);
        }));
        var t = Task.WhenAll(tasksList.ToArray()).ContinueWith((t) =>
        {
            Console.WriteLine(t);
        });
        var w = t.GetAwaiter();
        w.GetResult();
        Console.WriteLine($"end CheckDfListUrlResourceList");
    }
}