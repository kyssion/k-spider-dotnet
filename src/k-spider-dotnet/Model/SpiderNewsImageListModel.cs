using SqlSugar;

namespace KSpider.Model;

/// <summary>
///     爬虫详情中的图片信息记录
/// </summary>
[SugarTable("spider_news_image_list")]
public class SpiderNewsImageListModel : ILongIdEntity
{
    /// <summary>
    ///     Desc:
    ///     Default:nextval('spider_news_image_list_id_seq'::regclass)
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
    [SugarColumn(ColumnName = "image_resource_url")]
    public string? ImageResourceUrl { get; set; }

    /// <summary>
    ///     Desc:
    ///     Default:
    ///     Nullable:True
    /// </summary>
    [SugarColumn(ColumnName = "image_name")]
    public string? ImageName { get; set; }
}