using SqlSugar;

namespace KSpider.Model;

/// <summary>
///     官方文件 ( Policy 管线 , 第六管线 ) : 政府/官方机构发布的文件与政策——
///     文件元数据 ( 发文机关/文号 ) + 正文全文 ( 政策文本即核心语料 ) ;
///     发布即终态 ( DO NOTHING ) , 无状态机。契约见 docs/architecture.md。
/// </summary>
[SugarTable("spider_policy_document")]
public class SpiderPolicyDocumentModel : ILongIdEntity, IUpdateTimeEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "create_time")]
    public DateTime CreateTime { get; set; }

    [SugarColumn(ColumnName = "update_time")]
    public DateTime UpdateTime { get; set; }

    /// <summary>
    ///     来源 , 与 FromTypeOfNews 枚举对应 ( 10 统计局 / 11 证监会 , 每官方站一值 )
    /// </summary>
    [SugarColumn(ColumnName = "from_media")]
    public int FromMedia { get; set; }

    /// <summary>
    ///     源内文档唯一键 ( 统计局 = 路径日期段+文档号 , 证监会 = 栏目文档 id ) ,
    ///     与 from_media 组成去重键
    /// </summary>
    [SugarColumn(ColumnName = "doc_id")]
    public string DocId { get; set; } = "";

    [SugarColumn(ColumnName = "title")]
    public string Title { get; set; } = "";

    /// <summary>
    ///     发文机关 ( 官方文件核心维度 : 国家统计局 / 中国证监会 / 各部委… )
    /// </summary>
    [SugarColumn(ColumnName = "institution")]
    public string Institution { get; set; } = "";

    /// <summary>
    ///     文号 ( 如 "国统局字〔2026〕X 号" 形态 , 规范引用键 ; 提不到为空 )
    /// </summary>
    [SugarColumn(ColumnName = "document_number")]
    public string? DocumentNumber { get; set; }

    /// <summary>
    ///     发布时间 ( 精确到分/秒 , 列表仅有日期时由详情补齐 )
    /// </summary>
    [SugarColumn(ColumnName = "publish_time")]
    public DateTime PublishTime { get; set; }

    /// <summary>
    ///     正文页地址
    /// </summary>
    [SugarColumn(ColumnName = "url")]
    public string Url { get; set; } = "";

    /// <summary>
    ///     正文纯文本 ( 段落 + 表格文本化 ; 政策文本即核心语料 , 必须入库 )
    /// </summary>
    [SugarColumn(ColumnName = "content_text")]
    public string? ContentText { get; set; }

    /// <summary>
    ///     附件地址 , JSON 数组字符串 ( 统计局数据表 xls / 政策原文 PDF 等 )
    /// </summary>
    [SugarColumn(ColumnName = "attachments")]
    public string? Attachments { get; set; }

    /// <summary>
    ///     详情页原始 HTML ( 重跑能力 )
    /// </summary>
    [SugarColumn(ColumnName = "raw_content")]
    public string? RawContent { get; set; }
}
