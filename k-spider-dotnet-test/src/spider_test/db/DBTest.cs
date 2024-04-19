using k_spider_dotnet_test.spider_test.tool.developer;

namespace k_spider_dotnet_test.spider_test.db;

[TestClass]
public class DbTest
{
    [TestMethod]
    public void TestCreatePgModer()
    {
       BuildPg.CreatePgModer();
    }
}