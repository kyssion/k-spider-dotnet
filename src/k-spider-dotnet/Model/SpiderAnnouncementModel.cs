using SqlSugar;

namespace KSpider.Model;

/// <summary>
///     公告 ( Announcement 管线 ) : 交易所法定披露的结构化文档元数据——
///     证券代码 + 标题 + 分类 + PDF 附件链接 , 无正文 ( PDF 不下载 , 链接即交付物 ) ;
///     披露即终态 ( DO NOTHING ) , 无状态机。契约见 docs/architecture.md。
/// </summary>
[SugarTable("spider_announcement")]
public class SpiderAnnouncementModel : ILongIdEntity, IUpdateTimeEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "create_time")]
    public DateTime CreateTime { get; set; }

    [SugarColumn(ColumnName = "update_time")]
    public DateTime UpdateTime { get; set; }

    /// <summary>
    ///     站内公告唯一标识 ( 巨潮 announcementId ) , 去重键
    /// </summary>
    [SugarColumn(ColumnName = "announcement_id")]
    public string AnnouncementId { get; set; } = "";

    /// <summary>
    ///     来源 , 与 FromTypeOfNews 枚举对应 ( 9 巨潮 ; 港交所披露易接入时新增枚举 )
    /// </summary>
    [SugarColumn(ColumnName = "from_media")]
    public int FromMedia { get; set; }

    /// <summary>
    ///     证券代码 ( 基金/债券类公告可能为空 )
    /// </summary>
    [SugarColumn(ColumnName = "sec_code")]
    public string? SecCode { get; set; }

    [SugarColumn(ColumnName = "sec_name")]
    public string? SecName { get; set; }

    /// <summary>
    ///     公告分类 ( 巨潮 announcementType 分类代码串 , 对照巨潮分类表解读 )
    /// </summary>
    [SugarColumn(ColumnName = "category")]
    public string? Category { get; set; }

    [SugarColumn(ColumnName = "title")]
    public string Title { get; set; } = "";

    /// <summary>
    ///     PDF 附件完整地址 ( 链接即交付物 , 不下载不解析 )
    /// </summary>
    [SugarColumn(ColumnName = "pdf_url")]
    public string? PdfUrl { get; set; }

    /// <summary>
    ///     披露时间 ( 公告发布时刻 , 精确到秒 )
    /// </summary>
    [SugarColumn(ColumnName = "publish_time")]
    public DateTime PublishTime { get; set; }

    /// <summary>
    ///     原始响应条目 JSON , 保留长尾字段 ( orgId/columnId/multi 证券清单等 ) 与解析重跑能力
    /// </summary>
    [SugarColumn(ColumnName = "raw_content")]
    public string? RawContent { get; set; }
}
