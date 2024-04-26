using k_spider_dotnet.tool.log;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace k_spider_dotnet.dal.db;

public class Pg
{
    private const string PgConnectionString =
        "PORT=5432;DATABASE=k_script_spider;HOST=39.100.86.193;PASSWORD=Javarustc++11.;USER ID=spider ;Include Error Detail=true;Pooling=true;MaxPoolSize=10";
    private static readonly ILogger Log = LogFactory.GetLogger<Pg>();

    // 创建 pg 链接信息
    public static SqlSugarClient Connection(string connectionString = Pg.PgConnectionString,bool isAutoCloseConnection = true)
    {
        return new SqlSugarClient(new ConnectionConfig()
            {
                ConnectionString = connectionString,
                DbType = DbType.PostgreSQL,
                IsAutoCloseConnection = isAutoCloseConnection,
            },
            db => {
                db.Aop.OnError = (exp) =>//SQL报错
                {
                    UtilMethods.GetSqlString(DbType.SqlServer, exp.Sql, exp.Parametres as SugarParameter[]);
                };
            });
    }
}