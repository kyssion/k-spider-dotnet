using SqlSugar;

namespace KSpider.Model;

/// <summary>
///     新闻信息详情表
/// </summary>
[SugarTable("spider_news_content")]
public class SpiderNewsContentModel : ILongIdEntity
{
    /// <summary>
    ///     Desc:
    ///     Default:nextval('spider_news_content_id_seq'::regclass)
    ///     Nullable:False
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
    public long Id { get; set; }

    /// <summary>
    ///     Desc:
    ///     Default:DateTime.Now
    ///     Nullable:False
    /// </summary>
    [SugarColumn(ColumnName = "create_time")]
    public DateTime CreateTime { get; set; }

    /// <summary>
    ///     Desc:
    ///     Default:DateTime.Now
    ///     Nullable:False
    /// </summary>
    [SugarColumn(ColumnName = "update_time")]
    public DateTime UpdateTime { get; set; }

    /// <summary>
    ///     Desc:
    ///     Default:
    ///     Nullable:True
    /// </summary>
    [SugarColumn(ColumnName = "news_url")]
    public string? NewsUrl { get; set; }

    /// <summary>
    ///     Desc:
    ///     Default:
    ///     Nullable:True
    /// </summary>
    [SugarColumn(ColumnName = "news_title")]
    public string? NewsTitle { get; set; }

    /// <summary>
    ///     Desc:
    ///     Default:
    ///     Nullable:True
    /// </summary>
    [SugarColumn(ColumnName = "news_summary")]
    public string? NewsSummary { get; set; }

    /// <summary>
    ///     Desc:
    ///     Default:
    ///     Nullable:True
    /// </summary>
    [SugarColumn(ColumnName = "news_from")]
    public string? NewsFrom { get; set; }

    /// <summary>
    ///     Desc:
    ///     Default:
    ///     Nullable:True
    /// </summary>
    [SugarColumn(ColumnName = "news_time")]
    public DateTime? NewsTime { get; set; }

    /// <summary>
    ///     Desc:
    ///     Default:
    ///     Nullable:True
    /// </summary>
    [SugarColumn(ColumnName = "news_keyword")]
    public string? NewsKeyword { get; set; }

    /// <summary>
    ///     Desc:
    ///     Default:
    ///     Nullable:True
    /// </summary>
    [SugarColumn(ColumnName = "news_content_json")]
    public string? NewsContentJson { get; set; }

    /// <summary>
    ///     Desc:
    ///     Default:
    ///     Nullable:True
    /// </summary>
    [SugarColumn(ColumnName = "news_content_text")]
    public string? NewsContentText { get; set; }
}