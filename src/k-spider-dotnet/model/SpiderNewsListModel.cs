using SqlSugar;

namespace k_spider_dotnet.model;

/// <summary>
///     排重抓取信息信息列表
/// </summary>
[SugarTable("spider_news_list")]
public class SpiderNewsListModel : ILongIdEntity
{
    /// <summary>
    ///     Desc:
    ///     Default:nextval('spider_news_list_id_seq'::regclass)
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
    [SugarColumn(ColumnName = "from_media")]
    public int? FromMedia { get; set; }

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
    [SugarColumn(ColumnName = "news_download_time")]
    public DateTime? NewsDownloadTime { get; set; }

    /// <summary>
    ///     Desc:新闻类型
    ///     Default:0
    ///     Nullable:False
    /// </summary>
    [SugarColumn(ColumnName = "category")]
    public int Category { get; set; }

    /// <summary>
    ///     Desc:详情数据是否下载 0 没有下载 1 已下载
    ///     Default:0
    ///     Nullable:False
    /// </summary>
    [SugarColumn(ColumnName = "download_status_code")]
    public int DownloadStatusCode { get; set; }

    /// <summary>
    ///     Desc:流水线失败重试次数 , 达到上限后不再重试
    ///     Default:0
    ///     Nullable:False
    /// </summary>
    [SugarColumn(ColumnName = "fail_count")]
    public int FailCount { get; set; }
}