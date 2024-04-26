using k_spider_dotnet.tool.resource;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace k_spider_dotnet_test.spider_test.tool;

[TestClass]
public class StringToolsTest
{
    [TestMethod]
    public void Test()
    {
        Console.WriteLine(        StringTools.UnderlineToCamelCase("qwe_qwe_qwe_",true));
    }
}