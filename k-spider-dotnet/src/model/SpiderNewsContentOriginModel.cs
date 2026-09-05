using SqlSugar;

namespace k_spider_dotnet.model;

/// <summary>
/// </summary>
[SugarTable("spider_news_content_origin")]
public class SpiderNewsContentOriginModel
{
    /// <summary>
    ///     Desc:
    ///     Default:nextval('spider_news_content_origin_id_seq'::regclass)
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
    ///     Nullable:False
    /// </summary>
    [SugarColumn(ColumnName = "news_url")]
    public string? NewsUrl { get; set; }

    /// <summary>
    ///     Desc:
    ///     Default:
    ///     Nullable:True
    /// </summary>
    [SugarColumn(ColumnName = "news_origin_content")]
    public string? NewsOriginContent { get; set; }

    /// <summary>
    ///     Desc:
    ///     Default:0
    ///     Nullable:False
    /// </summary>
    [SugarColumn(ColumnName = "news_origin_type")]
    public int NewsOriginType { get; set; }

    /// <summary>
    ///     Desc:
    ///     Default:0
    ///     Nullable:False
    /// </summary>
    [SugarColumn(ColumnName = "status")]
    public int Status { get; set; }

    /// <summary>
    ///     Desc:
    ///     Default:
    ///     Nullable:True
    /// </summary>
    [SugarColumn(ColumnName = "message")]
    public string? Message { get; set; }
}