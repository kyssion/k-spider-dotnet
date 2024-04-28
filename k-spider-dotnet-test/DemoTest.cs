using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace k_spider_dotnet_test;

[TestClass]
public class DemoTest
{
    [TestMethod]
    public void DemoTestMethod()
    {
        var start = DateTime.Now;
        var list = new List<StringBuilder>();
        for (var a = 0; a < 10_000_000; a++)
        {
            var item = new StringBuilder(100);
            item.Append("hello world" + a);
            list.Add(item);
        }

        Console.WriteLine(DateTime.Now.Subtract(start).TotalMilliseconds);
        Console.WriteLine(list.Count);
    }
}