using SqlSugar;

namespace KSpider.Model;

/// <summary>
///     实时快讯 ( 财联社电报 / 新浪 7x24 / 见闻 live / 金十快讯这类"列表即全文"的源 )。
///     与网页抓取型新闻 ( 三张表 + 状态机 ) 不同 : 拉到即完整数据 , 入库即终态 , 无下载/解析阶段。
/// </summary>
[SugarTable("spider_flash_news")]
public class SpiderFlashNewsModel : ILongIdEntity, IUpdateTimeEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "create_time")]
    public DateTime CreateTime { get; set; }

    [SugarColumn(ColumnName = "update_time")]
    public DateTime UpdateTime { get; set; }

    /// <summary>
    ///     来源 , 与 FromTypeOfNews 枚举对应 ( 2 财联社 / 3 新浪 / 4 见闻 / 5 金十 )
    /// </summary>
    [SugarColumn(ColumnName = "from_media")]
    public int FromMedia { get; set; }

    /// <summary>
    ///     栏目编号 , 沿用各源独立编号段 ( 101 / 201 / 301 / 401 )
    /// </summary>
    [SugarColumn(ColumnName = "category")]
    public int Category { get; set; }

    /// <summary>
    ///     稳定形态 URL ( 如 https://www.cls.cn/detail/{id} ) , 与 from_media 组成去重键
    /// </summary>
    [SugarColumn(ColumnName = "news_url")]
    public string NewsUrl { get; set; } = "";

    /// <summary>
    ///     发布时间 , 各源统一换算为东八区后落库
    /// </summary>
    [SugarColumn(ColumnName = "news_time")]
    public DateTime NewsTime { get; set; }

    /// <summary>
    ///     标题 ( 无标题源用正文截断 60 字 )
    /// </summary>
    [SugarColumn(ColumnName = "title")]
    public string? Title { get; set; }

    /// <summary>
    ///     正文全文
    /// </summary>
    [SugarColumn(ColumnName = "content")]
    public string? Content { get; set; }

    /// <summary>
    ///     标签 ( 财联社 subjects / 新浪 tag / 金十 tags ) , 逗号分隔
    /// </summary>
    [SugarColumn(ColumnName = "keyword")]
    public string? Keyword { get; set; }

    /// <summary>
    ///     重要度 : 1 普通 / 2 重要 / 3 重大 ; 各源映射见 docs/news-pipeline.md
    /// </summary>
    [SugarColumn(ColumnName = "level")]
    public short Level { get; set; }

    /// <summary>
    ///     关联标的 , JSON 数组字符串 ( 如 [{"stock_id":"sz300476","name":"胜宏科技"}] )
    /// </summary>
    [SugarColumn(ColumnName = "stock_list")]
    public string? StockList { get; set; }

    /// <summary>
    ///     图片 URL , JSON 数组字符串 ( 快讯图片属低频 )
    /// </summary>
    [SugarColumn(ColumnName = "image_urls")]
    public string? ImageUrls { get; set; }

    /// <summary>
    ///     原始响应条目 JSON , 保留解析规则变更后的重跑能力 ( 不再单设 origin 表 )
    /// </summary>
    [SugarColumn(ColumnName = "raw_content")]
    public string? RawContent { get; set; }
}
