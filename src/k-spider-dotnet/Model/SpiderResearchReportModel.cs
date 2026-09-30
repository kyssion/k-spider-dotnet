using SqlSugar;

namespace KSpider.Model;

/// <summary>
///     研报 ( 东财研报中心 , 个股/行业/宏观三类 ) :
///     与网页抓取型新闻不同 —— 列表接口一次给全结构化元数据 , 单表直写 ;
///     摘要正文由详情页二段回填 ( summary_fail_count 控制重试 ) , 无状态机、无 origin 表。
///     第三条管线专属表 , 不入新闻三表。
/// </summary>
[SugarTable("spider_research_report")]
public class SpiderResearchReportModel : ILongIdEntity, IUpdateTimeEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "create_time")]
    public DateTime CreateTime { get; set; }

    [SugarColumn(ColumnName = "update_time")]
    public DateTime UpdateTime { get; set; }

    /// <summary>
    ///     东财研报唯一标识 ( 如 AP202609301830020241 ) , 去重键
    /// </summary>
    [SugarColumn(ColumnName = "info_code")]
    public string InfoCode { get; set; } = "";

    /// <summary>
    ///     来源 , 与 FromTypeOfNews 枚举对应 ( 当前固定 1 东财 , 预留多源 )
    /// </summary>
    [SugarColumn(ColumnName = "from_media")]
    public int FromMedia { get; set; }

    /// <summary>
    ///     研报类型 : 1 个股 / 2 行业 / 3 宏观 ( 对应接口 qType 0/1/2 )
    /// </summary>
    [SugarColumn(ColumnName = "report_kind")]
    public short ReportKind { get; set; }

    /// <summary>
    ///     报告标题
    /// </summary>
    [SugarColumn(ColumnName = "title")]
    public string Title { get; set; } = "";

    /// <summary>
    ///     个股代码 ( 仅个股研报 , 如 688548 )
    /// </summary>
    [SugarColumn(ColumnName = "stock_code")]
    public string? StockCode { get; set; }

    /// <summary>
    ///     个股名称
    /// </summary>
    [SugarColumn(ColumnName = "stock_name")]
    public string? StockName { get; set; }

    /// <summary>
    ///     研究机构简称 ( 如 万联证券 )
    /// </summary>
    [SugarColumn(ColumnName = "org_name")]
    public string? OrgName { get; set; }

    /// <summary>
    ///     东财评级名 ( 增持 / 买入 / 中性… , 宏观研报为空 )
    /// </summary>
    [SugarColumn(ColumnName = "rating_name")]
    public string? RatingName { get; set; }

    /// <summary>
    ///     行业名 ( 个股研报为细分行业 , 行业研报为所属行业 , 宏观研报为空 )
    /// </summary>
    [SugarColumn(ColumnName = "industry_name")]
    public string? IndustryName { get; set; }

    /// <summary>
    ///     目标价上限 ( 仅个股研报 , 空 = 未给 )
    /// </summary>
    [SugarColumn(ColumnName = "aim_price_high")]
    public decimal? AimPriceHigh { get; set; }

    /// <summary>
    ///     目标价下限
    /// </summary>
    [SugarColumn(ColumnName = "aim_price_low")]
    public decimal? AimPriceLow { get; set; }

    /// <summary>
    ///     预测当年 EPS
    /// </summary>
    [SugarColumn(ColumnName = "eps_this_year")]
    public decimal? EpsThisYear { get; set; }

    /// <summary>
    ///     预测当年 PE
    /// </summary>
    [SugarColumn(ColumnName = "pe_this_year")]
    public decimal? PeThisYear { get; set; }

    /// <summary>
    ///     预测明年 EPS
    /// </summary>
    [SugarColumn(ColumnName = "eps_next_year")]
    public decimal? EpsNextYear { get; set; }

    /// <summary>
    ///     预测明年 PE
    /// </summary>
    [SugarColumn(ColumnName = "pe_next_year")]
    public decimal? PeNextYear { get; set; }

    /// <summary>
    ///     分析师 ( 逗号分隔 )
    /// </summary>
    [SugarColumn(ColumnName = "researcher")]
    public string? Researcher { get; set; }

    /// <summary>
    ///     摘要正文 ( 详情页 ctx-content 区的纯文本段落 , 二段回填 )
    /// </summary>
    [SugarColumn(ColumnName = "summary")]
    public string? Summary { get; set; }

    /// <summary>
    ///     摘要回填失败次数 ( 达 NewsPipelineConst.MaxFailCount 后不再重试 )
    /// </summary>
    [SugarColumn(ColumnName = "summary_fail_count")]
    public int SummaryFailCount { get; set; }

    /// <summary>
    ///     发布日期 ( 接口只到日期级 , 时间恒为 00:00:00 )
    /// </summary>
    [SugarColumn(ColumnName = "publish_date")]
    public DateTime PublishDate { get; set; }

    /// <summary>
    ///     原始响应条目 JSON , 保留长尾字段 ( 历史预测期 / 附件信息 / encodeUrl 等 ) 与解析重跑能力
    /// </summary>
    [SugarColumn(ColumnName = "raw_content")]
    public string? RawContent { get; set; }
}
