using k_spider_dotnet.logger;
using k_spider_dotnet.config;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace k_spider_dotnet.data;

public class Pg
{
    private static readonly ILogger Log = LogFactory.GetLogger<Pg>();

    // 创建 pg 链接信息 , 连接串统一走 AppConfig ( appsettings.json / K_SPIDER_ 环境变量 )
    public static SqlSugarClient Connection(string connectionString = "",
        bool isAutoCloseConnection = true)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = AppConfig.Database.ConnectionString;
        return new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = connectionString,
            DbType = DbType.PostgreSQL,
            IsAutoCloseConnection = isAutoCloseConnection
        },
        db =>
        {
            db.Aop.OnError = exp => //SQL报错
            {
                Log.LogError("sql err : {} , sql : {}", exp.Message, exp.Sql);
            };
        });
    }

    /// <summary>
    ///     程序启动时的幂等库结构补齐 : fail_count 列 + 流水线轮询部分索引。
    ///     表结构本体仍由 k-script-script-datasource.sql 初始化 , 这里只做增量演进。
    /// </summary>
    public static void EnsureSpiderNewsListDbObjects()
    {
        try
        {
            using var connection = Connection();
            connection.Ado.ExecuteCommand(
                "ALTER TABLE public.spider_news_list ADD COLUMN IF NOT EXISTS fail_count integer DEFAULT 0 NOT NULL");
            connection.Ado.ExecuteCommand(
                """
                CREATE INDEX IF NOT EXISTS idx_news_list_download_status
                    ON public.spider_news_list (download_status_code, id)
                    WHERE download_status_code IN (0, 2, 3, 4)
                """);
        }
        catch (Exception e)
        {
            // 启动时数据库不可用不阻断进程 , Job 轮询会持续重试
            Log.LogError("[EnsureSpiderNewsListDbObjects] err : {}", e);
        }
    }
}