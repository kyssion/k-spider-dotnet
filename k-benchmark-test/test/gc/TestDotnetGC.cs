using System.Diagnostics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace k;

[TestClass]
public class TestDotnetGc
{
    /// <summary>
    ///     .net 8.0 创建大量小对象 , 性能依赖gc性能
    /// </summary>
    [TestMethod]
    public void TestCreateObjectGc()
    {
       
        
    }
}