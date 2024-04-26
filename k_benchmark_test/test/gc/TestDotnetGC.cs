using System.Diagnostics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace k_benchmark_test.test.gc;

[TestClass]
public class TestDotnetGc
{
    /// <summary>
    ///  .net 8.0 创建大量小对象 , 性能依赖gc性能
    /// </summary>
    [TestMethod]
    public void TestCreateObjectGc()
    {
        var st = new Stopwatch();
        st.Start();
        List<string> strings= new List<string>();
        for (var i = 0; i < 100_000_000; i++)
        {
            StringBuilder item = new StringBuilder("hello world");
            strings.Add(item.Append(i).ToString());
        }
        st.Stop();
        Console.WriteLine(st.ElapsedMilliseconds);
        Console.WriteLine(strings.Count);
    }
}