using KSpider.Config;
using KSpider.Logger;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace KSpider.Data;

/// <summary>
///     SqlSugar 连接工厂 , DI 注册为 Singleton
/// </summary>
public class Pg
{
    private static readonly ILogger Log = LogFactory.GetLogger<Pg>();

    private readonly DatabaseOptions _options;

    public Pg(IOptions<DatabaseOptions> options)
    {
        _options = options.Value;
    }

    // 创建 pg 链接信息 ( 使用配置的默认连接串 , 配置为空时回退本地开发默认值 )
    public SqlSugarClient Connection()
    {
        var connectionString = string.IsNullOrWhiteSpace(_options.ConnectionString)
            ? DatabaseOptions.DefaultConnectionString
            : _options.ConnectionString;
        return CreateConnection(connectionString);
    }

    /// <summary>
    ///     用指定连接串创建连接 ( k-spider-sync 的远端 / 本地库等场景 )
    /// </summary>
    public static SqlSugarClient CreateConnection(string connectionString)
    {
        return new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = connectionString,
                DbType = DbType.PostgreSQL,
                IsAutoCloseConnection = true
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
    ///     表结构本体仍由 db/k-script-spider-datasource.sql 初始化 , 这里只做增量演进。
    /// </summary>
    public void EnsureSpiderNewsListDbObjects()
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
