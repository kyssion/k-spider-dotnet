using SqlSugar;

namespace KSpider.Model;

/// <summary>
///     盘面榜单 ( Ranking 管线 , 第三…第四条管线 ) :
///     交易所/数据商按交易日披露的周期性数值快照 ( 龙虎榜/大宗交易/两融 ) ,
///     行即数值无正文 ; 发布即终态 ( DO NOTHING ) , 无状态机无回填。
///     类型内自然键 row_key 见 Ranking/Eastmoney 的类型配置 ( 契约见 docs/architecture.md )。
/// </summary>
[SugarTable("spider_ranking")]
public class SpiderRankingModel : ILongIdEntity, IUpdateTimeEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "create_time")]
    public DateTime CreateTime { get; set; }

    [SugarColumn(ColumnName = "update_time")]
    public DateTime UpdateTime { get; set; }

    /// <summary>
    ///     榜单类型 : 1 龙虎榜 / 2 大宗交易 / 3 两融 / 4 北向 ( 预留 , 2024-08 起无每日披露 )
    /// </summary>
    [SugarColumn(ColumnName = "ranking_type")]
    public short RankingType { get; set; }

    /// <summary>
    ///     交易日 ( 数据所属日期 , 非披露日 )
    /// </summary>
    [SugarColumn(ColumnName = "trade_date")]
    public DateTime TradeDate { get; set; }

    /// <summary>
    ///     类型内自然键 ( 龙虎榜=代码+上榜原因 / 大宗=代码+买卖营业部+价格 / 两融=代码 ) ,
    ///     与 ( ranking_type, trade_date ) 组成去重键
    /// </summary>
    [SugarColumn(ColumnName = "row_key")]
    public string RowKey { get; set; } = "";

    /// <summary>
    ///     来源 , 与 FromTypeOfNews 枚举对应 ( 恒 1 东财数据中心 , reportName 接口族同站 )
    /// </summary>
    [SugarColumn(ColumnName = "from_media")]
    public int FromMedia { get; set; }

    [SugarColumn(ColumnName = "stock_code")]
    public string? StockCode { get; set; }

    [SugarColumn(ColumnName = "stock_name")]
    public string? StockName { get; set; }

    [SugarColumn(ColumnName = "market")]
    public string? Market { get; set; }

    [SugarColumn(ColumnName = "close_price")]
    public decimal? ClosePrice { get; set; }

    /// <summary>涨跌幅 % ( 两融无 )</summary>
    [SugarColumn(ColumnName = "change_rate")]
    public decimal? ChangeRate { get; set; }

    /// <summary>成交额 : 龙虎榜=榜内成交额 / 大宗=大宗成交额 ( 两融无 )</summary>
    [SugarColumn(ColumnName = "deal_amount")]
    public decimal? DealAmount { get; set; }

    /// <summary>净额 : 龙虎榜=榜内净买入 ( 大宗/两融无 )</summary>
    [SugarColumn(ColumnName = "net_amount")]
    public decimal? NetAmount { get; set; }

    /// <summary>买入额 : 龙虎榜=榜内买入额 ( 大宗/两融无 )</summary>
    [SugarColumn(ColumnName = "buy_amount")]
    public decimal? BuyAmount { get; set; }

    /// <summary>卖出额 : 龙虎榜=榜内卖出额 ( 大宗/两融无 )</summary>
    [SugarColumn(ColumnName = "sell_amount")]
    public decimal? SellAmount { get; set; }

    /// <summary>
    ///     类型特有长尾字段 ( JSONB ) : 龙虎榜=后市表现/席位解释 , 大宗=买卖营业部/溢价率 ,
    ///     两融=融资余额/融券余额/两融余额等 ; 下游按 ranking_type 取键
    /// </summary>
    [SugarColumn(ColumnName = "detail")]
    public string? Detail { get; set; }

    /// <summary>
    ///     原始响应条目 JSON ( 与快讯/研报同款 , 解析重跑能力 )
    /// </summary>
    [SugarColumn(ColumnName = "raw_content")]
    public string? RawContent { get; set; }
}
