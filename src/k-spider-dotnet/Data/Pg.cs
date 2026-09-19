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
            CheckBatchUpsertUniqueIndexes(connection);
        }
        catch (Exception e)
        {
            // 启动时数据库不可用不阻断进程 , Job 轮询会持续重试
            Log.LogError("[EnsureSpiderNewsListDbObjects] err : {}", e);
        }
    }

    /// <summary>
    ///     批量 upsert 依赖的唯一索引自检。
    ///     SpiderNewsBatchDao 的手拼 SQL 用 ON CONFLICT (列) 做去重 , 该列上必须有唯一索引 / 约束 ,
    ///     否则 PostgreSQL 会报 "there is no unique or exclusion constraint matching the ON CONFLICT
    ///     specification" , 而列表任务里列表行与原始内容同事务 , 异常会把整批写入一起回滚 ——
    ///     表现为该源"一行都进不来"却不影响其它源 , 排查成本很高 , 因此启动时显式告警。
    /// </summary>
    private static void CheckBatchUpsertUniqueIndexes(SqlSugarClient connection)
    {
        var requiredIndexes = new (string Table, string Column)[]
        {
            ("spider_news_list", "news_url"),
            ("spider_news_content_origin", "news_url"),
            ("spider_news_content", "news_url"),
            ("spider_news_image_list", "image_resource_url")
        };
        foreach (var (table, column) in requiredIndexes)
        {
            var count = connection.Ado.GetInt(
                """
                SELECT count(*) FROM pg_index i
                JOIN pg_class t ON t.oid = i.indrelid
                JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY (i.indkey)
                WHERE t.relname = @table AND a.attname = @column AND i.indisunique
                """, new { table, column });
            if (count == 0)
                Log.LogError(
                    "[CheckBatchUpsertUniqueIndexes] 表 {Table} 的 {Column} 缺少唯一索引 , 批量 upsert 会整批失败 ( 该源将无数据落库 ) , 请按 db/k-script-spider-datasource.sql 补建",
                    table, column);
        }
    }
}
