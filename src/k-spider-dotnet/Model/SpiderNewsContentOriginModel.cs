using SqlSugar;

namespace KSpider.Model;

/// <summary>
/// </summary>
[SugarTable("spider_news_content_origin")]
public class SpiderNewsContentOriginModel : ILongIdEntity, IUpdateTimeEntity
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

    /// <summary>
    ///     原始内容是否来自付费/会员专享文章 ( 详情侧标记 , 如见闻 is_priced ) ; 与列表侧的 spider_news_list.is_paid 同步观察
    /// </summary>
    [SugarColumn(ColumnName = "is_paid")]
    public bool IsPaid { get; set; }
}
