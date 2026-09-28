using KSpider.Data;
using SqlSugar;

namespace KSpider.Web.Query;

/// <summary>
///     数据分析聚合 ( 只读 ) : 入库量趋势 / 分布 / 关键词词频。
///     所有聚合强制时间窗 ( ≤ 31 天 ) , 防全表聚合拖库。
/// </summary>
public class AnalysisQueryService(Pg pg)
{
    /// <summary>时间窗上限 ( 天 ) : 超过直接拒绝 , 保护库</summary>
    public const int MaxWindowDays = 31;

    private static readonly string[] Granularities = ["hour", "day"];

    /// <summary>入库量趋势 : 网页新闻与快讯各一条序列 , 按 granularity 分桶</summary>
    public VolumeDto Volume(DateTime start, DateTime end, string granularity)
    {
        (start, end, granularity) = NormalizeWindow(start, end, granularity);
        using var connection = pg.Connection();
        var news = QueryVolume(connection, "spider_news_list", start, end, granularity);
        var flash = QueryVolume(connection, "spider_flash_news", start, end, granularity);
        return new VolumeDto(news, flash);
    }

    /// <summary>分布 : 网页新闻按源 / 栏目 , 快讯按源 / 重要度</summary>
    public DistributeDto Distribute(DateTime start, DateTime end)
    {
        (start, end, _) = NormalizeWindow(start, end, "day");
        using var connection = pg.Connection();
        var newsSource = connection.Ado.SqlQuery<CountRow>("""
            SELECT from_media AS "Key", count(*) AS "Count"
            FROM spider_news_list
            WHERE news_time >= @start AND news_time < @end AND news_time IS NOT NULL
            GROUP BY from_media ORDER BY 2 DESC
            """, Params(start, end));
        var newsCategory = connection.Ado.SqlQuery<CountRow>("""
            SELECT category::text AS "Key", count(*) AS "Count"
            FROM spider_news_list
            WHERE news_time >= @start AND news_time < @end AND news_time IS NOT NULL
            GROUP BY category ORDER BY 2 DESC
            """, Params(start, end));
        var flashSource = connection.Ado.SqlQuery<CountRow>("""
            SELECT from_media::text AS "Key", count(*) AS "Count"
            FROM spider_flash_news
            WHERE news_time >= @start AND news_time < @end
            GROUP BY from_media ORDER BY 2 DESC
            """, Params(start, end));
        var flashLevel = connection.Ado.SqlQuery<CountRow>("""
            SELECT level::text AS "Key", count(*) AS "Count"
            FROM spider_flash_news
            WHERE news_time >= @start AND news_time < @end
            GROUP BY level ORDER BY 2 DESC
            """, Params(start, end));
        return new DistributeDto(newsSource, newsCategory, flashSource, flashLevel);
    }

    /// <summary>关键词词频 TopN : 快讯 keyword 与网页新闻 news_keyword ( 在 content 表 ) 按逗号拆分合并统计</summary>
    public List<CountRow> Keywords(DateTime start, DateTime end, int top)
    {
        (start, end, _) = NormalizeWindow(start, end, "day");
        top = Math.Clamp(top, 1, 100);
        using var connection = pg.Connection();
        return connection.Ado.SqlQuery<CountRow>("""
            SELECT word AS "Key", count(*) AS "Count"
            FROM (
                SELECT unnest(string_to_array(keyword, ',')) AS word
                FROM spider_flash_news
                WHERE news_time >= @start AND news_time < @end AND keyword IS NOT NULL
                UNION ALL
                SELECT unnest(string_to_array(news_keyword, ','))
                FROM spider_news_content
                WHERE news_time >= @start AND news_time < @end AND news_keyword IS NOT NULL
            ) words
            WHERE btrim(word) <> ''
            GROUP BY word
            ORDER BY count(*) DESC
            LIMIT @top
            """, new { start, end, top });
    }

    private static List<VolumeRow> QueryVolume(SqlSugarClient connection, string table,
        DateTime start, DateTime end, string granularity)
    {
        return connection.Ado.SqlQuery<VolumeRow>($"""
            SELECT date_trunc(@granularity, news_time) AS "Bucket", from_media::text AS "FromMedia", count(*) AS "Count"
            FROM {table}
            WHERE news_time >= @start AND news_time < @end AND news_time IS NOT NULL
            GROUP BY 1, 2 ORDER BY 1
            """, new { granularity, start, end });
    }

    /// <summary>
    ///     时间窗与粒度校一 : 默认最近 7 天 ; 窗口超上限抛 ArgumentException ( 端点转 400 ) ;
    ///     时间统一 SpecifyKind 为 Unspecified ( 列是 timestamp without time zone , 带 Kind 的参数会被 Npgsql 当 timestamptz )
    /// </summary>
    private static (DateTime Start, DateTime End, string Granularity) NormalizeWindow(
        DateTime start, DateTime end, string granularity)
    {
        granularity = Granularities.Contains(granularity) ? granularity : "day";
        start = DateTime.SpecifyKind(start, DateTimeKind.Unspecified);
        end = DateTime.SpecifyKind(end, DateTimeKind.Unspecified);
        if (end <= start) throw new ArgumentException("结束时间必须晚于开始时间");
        if ((end - start).TotalDays > MaxWindowDays)
            throw new ArgumentException($"时间窗最大 {MaxWindowDays} 天");
        return (start, end, granularity);
    }

    /// <summary>分页/聚合 SQL 的公共时间参数</summary>
    private static object Params(DateTime start, DateTime end)
    {
        return new { start, end };
    }
}

/// <summary>趋势序列的一个点 : 分桶时间 + 源编号 + 行数</summary>
public sealed class VolumeRow
{
    public DateTime Bucket { get; set; }
    public string FromMedia { get; set; } = "";
    public long Count { get; set; }
}

/// <summary>计数行 : Key 为分组键 ( 源 / 栏目 / 重要度 / 词 )</summary>
public sealed class CountRow
{
    public string Key { get; set; } = "";
    public long Count { get; set; }
}

/// <summary>入库量趋势 : 网页新闻与快讯两条序列</summary>
public sealed record VolumeDto(List<VolumeRow> News, List<VolumeRow> Flash);

/// <summary>分布统计 : 四个维度各一组计数</summary>
public sealed record DistributeDto(
    List<CountRow> NewsSource,
    List<CountRow> NewsCategory,
    List<CountRow> FlashSource,
    List<CountRow> FlashLevel);
