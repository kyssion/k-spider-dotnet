using System.Configuration;
using k_spider_dotnet.tool.resource;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using NetTaste;
using SqlSugar;

namespace k_spider_dotnet_test.spider_test.db;

[TestClass]
public class DbTest
{
    [TestMethod]
    public  void TestSqlSurge()
    {
        // jdbc url => postgresql://spider:Javarustc++11.@39.100.86.193:5432/k_script_spider
        //无需配置任何东西
        SqlSugarClient db = new SqlSugarClient(new ConnectionConfig()
        {
            DbType = DbType.PostgreSQL,
            ConnectionString = "PORT=5432;DATABASE=k_script_spider;HOST=39.100.86.193;PASSWORD=Javarustc++11.;USER ID=spider",
            IsAutoCloseConnection = true
        });
        db.DbFirst.IsCreateAttribute()
            .IsCreateAttribute()//创建sqlsugar自带特性
            .FormatFileName(it => StringTools.UnderlineToCamelCase(it,true)+"Model") //格式化文件名（文件名和表名不一样情况）
            .FormatClassName(it =>StringTools.UnderlineToCamelCase(it,true)+"Model")//格式化类名 （类名和表名不一样的情况）
            .FormatPropertyName(it => StringTools.UnderlineToCamelCase(it,true))//格式化属性名 （属性名和字段名不一样情况）
            .StringNullable().CreateClassFile("/Users/bytedance/RiderProjects/k-spider-dotnet", "Models");
    } 
}