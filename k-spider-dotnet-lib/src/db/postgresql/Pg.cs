using System.Data;
using k_spider_dotnet_lib.logger;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace k_spider_dotnet_lib.db.postgresql;

public class Pg
{
    private const string PgConnectionString =
        "PORT=5432;DATABASE=k_script_spider;HOST=39.100.86.193;PASSWORD=Javarustc++11.;USER ID=spider ;Include Error Detail=true;Pooling=true;MaxPoolSize=10";

    private static readonly ILogger Log = LogFactory.GetLogger<Pg>();

    // 创建 pg 链接信息
    public static SqlSugarClient Connection(string connectionString = PgConnectionString,
        bool isAutoCloseConnection = true)
    {
        return new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = connectionString,
                DbType = SqlSugar.DbType.PostgreSQL,
                IsAutoCloseConnection = isAutoCloseConnection
            },
            db =>
            {
                db.Aop.OnError = exp => //SQL报错
                {
                    // Log.LogError("sql error : {}",UtilMethods.GetSqlString(DbType.SqlServer, exp.Sql, exp.Parametres as SugarParameter[]));
                };
                // db.Aop.OnLogExecuting = (sql, pars) => //SQL执行前
                // {
                //     //获取原生SQL推荐 5.1.4.63  性能OK
                //     //UtilMethods.GetNativeSql(sql,pars)
                //
                //     //获取无参数化SQL 影响性能只适合调试
                //     Log.LogInformation("sql info : {}",UtilMethods.GetSqlString(DbType.SqlServer, sql, pars));
                // };

                //SQL执行完
                // db.Aop.OnLogExecuted = (sql, pars) => 
                // {
                //     //执行完了可以输出SQL执行时间 (OnLogExecutedDelegate) 
                //     Console.Write("time:" + db.Ado.SqlExecutionTime.ToString()); 
                // };
            });
    }
}