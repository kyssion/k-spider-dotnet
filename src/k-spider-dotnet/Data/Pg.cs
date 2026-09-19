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
    ///     实时快讯表的幂等建表 ( 表 + 实时消费索引 + update_time 触发器 )。
    ///     新环境由 DDL 建 , 这里保证存量环境升级后启动即可用。
    /// </summary>
    public void EnsureFlashNewsDbObjects()
    {
        try
        {
            using var connection = Connection();
            // id 用 bigserial ( 与 DDL 文件其它表一致 ) : 序列随表自动创建 , 无需单独建序列
            connection.Ado.ExecuteCommand(
                """
                CREATE TABLE IF NOT EXISTS public.spider_flash_news
                (
                    id          bigserial    NOT NULL,
                    create_time timestamp    NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    update_time timestamp    NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    from_media  integer      NOT NULL,
                    category    integer      NOT NULL DEFAULT 0,
                    news_url    varchar(300) NOT NULL,
                    news_time   timestamp    NOT NULL,
                    title       varchar(300),
                    content     text,
                    keyword     varchar(500),
                    level       smallint     NOT NULL DEFAULT 1,
                    stock_list  text,
                    image_urls  text,
                    raw_content text,
                    CONSTRAINT spider_flash_news_pkey PRIMARY KEY (id),
                    CONSTRAINT uk_flash_news_media_url UNIQUE (from_media, news_url)
                )
                """);
            connection.Ado.ExecuteCommand(
                """
                CREATE INDEX IF NOT EXISTS idx_flash_news_media_time
                    ON public.spider_flash_news (from_media, news_time)
                """);
            // PG 的 CREATE TRIGGER 不支持 IF NOT EXISTS , 先删后建保证幂等
            connection.Ado.ExecuteCommand("DROP TRIGGER IF EXISTS update_modified_column ON public.spider_flash_news");
            connection.Ado.ExecuteCommand(
                """
                CREATE TRIGGER update_modified_column BEFORE UPDATE ON public.spider_flash_news
                    FOR EACH ROW EXECUTE FUNCTION public.update_time_func()
                """);
        }
        catch (Exception e)
        {
            Log.LogError("[EnsureFlashNewsDbObjects] err : {}", e);
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
