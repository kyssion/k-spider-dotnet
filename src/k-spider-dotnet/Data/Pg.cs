using KSpider.Config;
using KSpider.Common;
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
    ///     表结构本体仍由 db/k_script_spider.sql 初始化 , 这里只做增量演进。
    /// </summary>
    public void EnsureSpiderNewsListDbObjects()
    {
        try
        {
            using var connection = Connection();
            connection.Ado.ExecuteCommand(
                "ALTER TABLE public.spider_news_list ADD COLUMN IF NOT EXISTS fail_count integer DEFAULT 0 NOT NULL");
            // 付费内容标记 ( 列表与 origin 两表同语义 , 老库由启动幂等补齐 )
            connection.Ado.ExecuteCommand(
                "ALTER TABLE public.spider_news_list ADD COLUMN IF NOT EXISTS is_paid boolean DEFAULT false NOT NULL");
            connection.Ado.ExecuteCommand(
                "ALTER TABLE public.spider_news_content_origin ADD COLUMN IF NOT EXISTS is_paid boolean DEFAULT false NOT NULL");
            // origin 解析路由标记 : from_media 冗余自列表行 + parser_code 解析器标识 ( 空 = 回退注册表 )
            connection.Ado.ExecuteCommand(
                "ALTER TABLE public.spider_news_content_origin ADD COLUMN IF NOT EXISTS from_media integer DEFAULT 0 NOT NULL");
            connection.Ado.ExecuteCommand(
                "ALTER TABLE public.spider_news_content_origin ADD COLUMN IF NOT EXISTS parser_code text");
            // 存量回填 ( 幂等 : 只动 from_media=0 的行 ) : 媒体标识按列表行回填 , 解析器码按当前注册表版本 ( v1 ) 回填
            connection.Ado.ExecuteCommand(
                """
                UPDATE public.spider_news_content_origin o
                SET from_media = COALESCE(l.from_media, 0),
                    parser_code = CASE COALESCE(l.from_media, 0)
                        WHEN 1 THEN 'df-article-v1'
                        WHEN 2 THEN 'cls-article-v1'
                        WHEN 3 THEN 'sina-html-v1'
                        WHEN 4 THEN 'wscn-article-v1'
                        ELSE NULL END
                FROM public.spider_news_list l
                WHERE o.news_url = l.news_url AND o.from_media = 0
                """);
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
    ///     研报表的幂等建表 ( 表 + 唯一键 + 摘要回填部分索引 + update_time 触发器 )。
    ///     新环境由 DDL 建 , 这里保证存量环境升级后启动即可用。
    /// </summary>
    public void EnsureResearchReportDbObjects()
    {
        try
        {
            using var connection = Connection();
            connection.Ado.ExecuteCommand(
                """
                CREATE TABLE IF NOT EXISTS public.spider_research_report
                (
                    id                 bigserial    NOT NULL,
                    create_time        timestamp    NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    update_time        timestamp    NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    info_code          varchar(64)  NOT NULL,
                    from_media         integer      NOT NULL,
                    report_kind        smallint     NOT NULL,
                    title              varchar(500) NOT NULL,
                    stock_code         varchar(32),
                    stock_name         varchar(64),
                    org_name           varchar(128),
                    rating_name        varchar(32),
                    industry_name      varchar(64),
                    aim_price_high     numeric(12,2),
                    aim_price_low      numeric(12,2),
                    eps_this_year      numeric(12,4),
                    pe_this_year       numeric(12,2),
                    eps_next_year      numeric(12,4),
                    pe_next_year       numeric(12,2),
                    researcher         varchar(200),
                    summary            text,
                    summary_fail_count integer      NOT NULL DEFAULT 0,
                    publish_date       timestamp    NOT NULL,
                    raw_content        text,
                    CONSTRAINT spider_research_report_pkey PRIMARY KEY (id),
                    CONSTRAINT uk_research_report_info_code UNIQUE (info_code)
                )
                """);
            connection.Ado.ExecuteCommand(
                """
                CREATE INDEX IF NOT EXISTS idx_research_report_publish_date
                    ON public.spider_research_report (publish_date)
                """);
            // 摘要回填热路径 : 只扫待回填且未达失败上限的行 ( ResearchReportJob 每 5 分钟限量取一批 )
            connection.Ado.ExecuteCommand(
                """
                CREATE INDEX IF NOT EXISTS idx_research_report_pending_summary
                    ON public.spider_research_report (id)
                    WHERE summary IS NULL AND summary_fail_count < 3
                """);
            // PG 的 CREATE TRIGGER 不支持 IF NOT EXISTS , 先删后建保证幂等
            connection.Ado.ExecuteCommand("DROP TRIGGER IF EXISTS update_modified_column ON public.spider_research_report");
            connection.Ado.ExecuteCommand(
                """
                CREATE TRIGGER update_modified_column BEFORE UPDATE ON public.spider_research_report
                    FOR EACH ROW EXECUTE FUNCTION public.update_time_func()
                """);
        }
        catch (Exception e)
        {
            Log.LogError("[EnsureResearchReportDbObjects] err : {}", e);
        }
    }

    /// <summary>
    ///     盘面榜单表的幂等建表 ( 表 + 类型行键唯一索引 + 查询索引 + update_time 触发器 )。
    ///     契约见 docs/architecture.md 的 Ranking 类型契约 ; 新环境由 DDL 建 ,
    ///     这里保证存量环境升级后启动即可用。
    /// </summary>
    public void EnsureRankingDbObjects()
    {
        try
        {
            using var connection = Connection();
            connection.Ado.ExecuteCommand(
                """
                CREATE TABLE IF NOT EXISTS public.spider_ranking
                (
                    id           bigserial     NOT NULL,
                    create_time  timestamp     NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    update_time  timestamp     NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ranking_type smallint      NOT NULL,
                    trade_date   date          NOT NULL,
                    row_key      varchar(180)  NOT NULL,
                    from_media   integer       NOT NULL,
                    stock_code   varchar(16),
                    stock_name   varchar(64),
                    market       varchar(40),
                    close_price  numeric(12,3),
                    change_rate  numeric(10,4),
                    deal_amount  numeric(20,2),
                    net_amount   numeric(20,2),
                    buy_amount   numeric(20,2),
                    sell_amount  numeric(20,2),
                    detail       jsonb,
                    raw_content  text,
                    CONSTRAINT spider_ranking_pkey PRIMARY KEY (id),
                    CONSTRAINT uk_ranking_type_date_row UNIQUE (ranking_type, trade_date, row_key)
                )
                """);
            connection.Ado.ExecuteCommand(
                """
                CREATE INDEX IF NOT EXISTS idx_ranking_type_date
                    ON public.spider_ranking (ranking_type, trade_date DESC)
                """);
            // PG 的 CREATE TRIGGER 不支持 IF NOT EXISTS , 先删后建保证幂等
            connection.Ado.ExecuteCommand("DROP TRIGGER IF EXISTS update_modified_column ON public.spider_ranking");
            connection.Ado.ExecuteCommand(
                """
                CREATE TRIGGER update_modified_column BEFORE UPDATE ON public.spider_ranking
                    FOR EACH ROW EXECUTE FUNCTION public.update_time_func()
                """);
        }
        catch (Exception e)
        {
            Log.LogError("[EnsureRankingDbObjects] err : {}", e);
        }
    }

    /// <summary>
    ///     系统状态表的幂等建表 ( Web 控制台通道 : 任务调度态 / 任务指令 / 节点快照 , 写入见 SystemStatusDao )。
    ///     新环境由 DDL 建 , 这里保证存量环境升级后启动即可用。
    /// </summary>
    public void EnsureSystemDbObjects()
    {
        try
        {
            using var connection = Connection();
            connection.Ado.ExecuteCommand("""
                CREATE TABLE IF NOT EXISTS public.spider_job_state
                (
                    node_id             text      NOT NULL,
                    job_name            text      NOT NULL,
                    next_fire_time      timestamp,
                    is_paused           boolean   NOT NULL DEFAULT false,
                    last_fired_at       timestamp,
                    last_duration_ms    bigint,
                    last_success        boolean,
                    consecutive_failures integer  NOT NULL DEFAULT 0,
                    last_error          text,
                    last_stats          text,
                    create_time         timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    update_time         timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT spider_job_state_pkey PRIMARY KEY (node_id, job_name)
                )
                """);
            connection.Ado.ExecuteCommand("""
                CREATE TABLE IF NOT EXISTS public.spider_job_command
                (
                    id          bigserial NOT NULL,
                    node_id     text,
                    job_name    text      NOT NULL,
                    action      text      NOT NULL,
                    status      text      NOT NULL DEFAULT 'pending',
                    result      text,
                    create_time timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    update_time timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    consumed_at timestamp,
                    CONSTRAINT spider_job_command_pkey PRIMARY KEY (id)
                )
                """);
            // 指令轮询热路径 : 只扫 pending ( NodeStateJob 每 3 秒 )
            connection.Ado.ExecuteCommand(
                "CREATE INDEX IF NOT EXISTS idx_job_command_pending ON public.spider_job_command (id) WHERE status = 'pending'");
            connection.Ado.ExecuteCommand("""
                CREATE TABLE IF NOT EXISTS public.spider_node_status
                (
                    node_id     text      NOT NULL,
                    report_time timestamp NOT NULL,
                    payload     text,
                    create_time timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    update_time timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT spider_node_status_pkey PRIMARY KEY (node_id)
                )
                """);
            // PG 的 CREATE TRIGGER 不支持 IF NOT EXISTS , 先删后建保证幂等
            // 数据重放任务表 ( Web 控制台重放工具 )
            connection.Ado.ExecuteCommand("""
                CREATE TABLE IF NOT EXISTS public.spider_replay_log
                (
                    id           bigserial NOT NULL,
                    filter       text,
                    status       text      NOT NULL DEFAULT 'running',
                    total        integer   NOT NULL DEFAULT 0,
                    success_count integer  NOT NULL DEFAULT 0,
                    fail_count   integer   NOT NULL DEFAULT 0,
                    message      text,
                    create_time  timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    update_time  timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    finish_time  timestamp,
                    CONSTRAINT spider_replay_log_pkey PRIMARY KEY (id)
                )
                """);
            foreach (var table in (string[])["spider_job_state", "spider_job_command", "spider_node_status", "spider_replay_log"])
            {
                connection.Ado.ExecuteCommand($"DROP TRIGGER IF EXISTS update_modified_column ON public.{table}");
                connection.Ado.ExecuteCommand(
                    $"CREATE TRIGGER update_modified_column BEFORE UPDATE ON public.{table} " +
                    "FOR EACH ROW EXECUTE FUNCTION public.update_time_func()");
            }
        }
        catch (Exception e)
        {
            Log.LogError("[EnsureSystemDbObjects] err : {}", e);
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
                    "[CheckBatchUpsertUniqueIndexes] 表 {Table} 的 {Column} 缺少唯一索引 , 批量 upsert 会整批失败 ( 该源将无数据落库 ) , 请按 db/k_script_spider.sql 补建",
                    table, column);
        }
    }
}
