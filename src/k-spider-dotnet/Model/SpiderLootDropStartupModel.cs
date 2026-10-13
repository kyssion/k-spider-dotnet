using SqlSugar;

namespace KSpider.Model;

/// <summary>
///     数据集行 ( loot-drop.io 倒闭创业公司案例库 , 第七管线 Dataset 数据集型首源 ) :
///     Supabase PostgREST 直连的结构化记录 , 无 HTML 解析环节 ;
///     行会修订 ( 内容编辑 / 浏览数增长 ) , 与"拉到即终态"的新闻类写入语义相反 ——
///     靠 content_hash ( 全部业务字段的 SHA256 ) 做变更检测 , 只更新真变的行。
///     主键直接用源侧 id ( 非自增 ) ; JSON 数组/对象列存 JSON 文本 ( 与 raw_content 同款 , 不用 jsonb )。
/// </summary>
[SugarTable("spider_lootdrop_startup")]
public class SpiderLootDropStartupModel : IUpdateTimeEntity
{
    /// <summary>源侧主键 ( API 的 startups.id , 直接沿用 , 非库自增 )</summary>
    [SugarColumn(IsPrimaryKey = true, ColumnName = "id")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "create_time")]
    public DateTime CreateTime { get; set; }

    [SugarColumn(ColumnName = "update_time")]
    public DateTime UpdateTime { get; set; }

    /// <summary>来源 , 与 FromTypeOfNews 枚举对应 ( 当前固定 LootDropMedia , 预留同类多源各建表 )</summary>
    [SugarColumn(ColumnName = "from_media")]
    public int FromMedia { get; set; }

    [SugarColumn(ColumnName = "name")]
    public string Name { get; set; } = "";

    /// <summary>案例导语 ( 数百字符的完整段落 )</summary>
    [SugarColumn(ColumnName = "description")]
    public string? Description { get; set; }

    [SugarColumn(ColumnName = "sector")]
    public string? Sector { get; set; }

    [SugarColumn(ColumnName = "country")]
    public string? Country { get; set; }

    /// <summary>创始人 , JSON 数组文本 ( 如 ["Matt Barnard","Nate Storey"] )</summary>
    [SugarColumn(ColumnName = "founders")]
    public string? Founders { get; set; }

    /// <summary>投资机构 , JSON 数组文本</summary>
    [SugarColumn(ColumnName = "investors")]
    public string? Investors { get; set; }

    [SugarColumn(ColumnName = "start_year")]
    public int? StartYear { get; set; }

    [SugarColumn(ColumnName = "end_year")]
    public int? EndYear { get; set; }

    /// <summary>累计融资额 ( 美元 , 源侧单位即为美元 )</summary>
    [SugarColumn(ColumnName = "total_funding")]
    public long? TotalFunding { get; set; }

    /// <summary>重建难度评分 1-5</summary>
    [SugarColumn(ColumnName = "difficulty")]
    public int? Difficulty { get; set; }

    [SugarColumn(ColumnName = "difficulty_reason")]
    public string? DifficultyReason { get; set; }

    /// <summary>可扩展性评分 1-5</summary>
    [SugarColumn(ColumnName = "scalability")]
    public int? Scalability { get; set; }

    [SugarColumn(ColumnName = "scalability_reason")]
    public string? ScalabilityReason { get; set; }

    /// <summary>市场潜力档位 ( low / medium / high )</summary>
    [SugarColumn(ColumnName = "market_potential")]
    public string? MarketPotential { get; set; }

    [SugarColumn(ColumnName = "market_potential_reason")]
    public string? MarketPotentialReason { get; set; }

    /// <summary>死因分析 ( 完整段落 )</summary>
    [SugarColumn(ColumnName = "cause_of_death")]
    public string? CauseOfDeath { get; set; }

    /// <summary>失败教训清单 , JSON 数组文本</summary>
    [SugarColumn(ColumnName = "the_loot")]
    public string? TheLoot { get; set; }

    /// <summary>市场分析 ( 完整段落 )</summary>
    [SugarColumn(ColumnName = "market_analysis")]
    public string? MarketAnalysis { get; set; }

    /// <summary>"用今天的 AI 重建它"构想 , JSON 对象文本 ( name / concept )</summary>
    [SugarColumn(ColumnName = "pivot_idea")]
    public string? PivotIdea { get; set; }

    /// <summary>源侧创建时间 ( UTC , 去时区存纯时间戳 , 避免内联字面量的会话时区重解释 )</summary>
    [SugarColumn(ColumnName = "source_created_at")]
    public DateTime? SourceCreatedAt { get; set; }

    [SugarColumn(ColumnName = "status")]
    public string? Status { get; set; }

    /// <summary>主要死因枚举值 ( 如 Unit Economics / Ran Out of Cash )</summary>
    [SugarColumn(ColumnName = "primary_cause_of_death")]
    public string? PrimaryCauseOfDeath { get; set; }

    [SugarColumn(ColumnName = "product_type")]
    public string? ProductType { get; set; }

    /// <summary>源站浏览数 ( 会增长 , 是行"会修订"的典型字段 )</summary>
    [SugarColumn(ColumnName = "views")]
    public int? Views { get; set; }

    [SugarColumn(ColumnName = "condensed_value_prop")]
    public string? CondensedValueProp { get; set; }

    [SugarColumn(ColumnName = "condensed_cause_of_death")]
    public string? CondensedCauseOfDeath { get; set; }

    /// <summary>
    ///     全部业务字段的 SHA256 十六进制指纹 ( 不含 from_media 与三个库管理列 ) ,
    ///     upsert 的变更检测依据 : 哈希没变的行跳过 UPDATE
    /// </summary>
    [SugarColumn(ColumnName = "content_hash")]
    public string ContentHash { get; set; } = "";

    /// <summary>原始条目 JSON , 保留长尾字段与解析重跑能力 ( 与研报表同款 )</summary>
    [SugarColumn(ColumnName = "raw_content")]
    public string? RawContent { get; set; }
}
