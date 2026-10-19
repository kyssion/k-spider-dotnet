namespace KSpider.Spider.Ranking.Eastmoney;

/// <summary>
///     盘面榜单类型 ( 与接口 reportName 一一对应 , 加一类 = 加一行配置 + 一个解析函数 )
/// </summary>
public enum RankingType
{
    /// <summary>龙虎榜 ( 每日上榜个股席位与买卖额 )</summary>
    Lhb = 1,

    /// <summary>大宗交易 ( 逐笔 , 含买卖营业部与溢价率 )</summary>
    BlockTrade = 2,

    /// <summary>融资融券 ( 个股两融余额 , T+1 披露 )</summary>
    Margin = 3,

    /// <summary>北向资金 ( 预留 : 2024-08 起交易所停止每日披露 , 暂无数据源 )</summary>
    Northbound = 4
}

/// <summary>
///     东财数据中心盘面榜单源常量与类型配置 ( 2026-09-30 实测 ) :
///     datacenter api 的 reportName 接口族 , 匿名可访问 ( 带浏览器 UA + Referer ) ;
///     filter 日期列名每类型不同 ( 龙虎榜/大宗=TRADE_DATE , 两融=DATE ) ,
///     龙虎榜支持按金额排序 , 两融只能按 DATE 排序 ( 分页拉全靠 sortTypes=-1 从新到旧 )。
///     契约见 docs/architecture.md 的 Ranking 类型契约。
/// </summary>
public static class DfRankingResource
{
    public const string ParserCode = "df-ranking-v1";

    /// <summary>
    ///     datacenter 查询模板 : {0}=reportName {1}=排序列 {2}=日期列 {3}=单日日期 {4}=页大小 {5}=页码。
    ///     filter 只用单日等于语法——范围 ( &gt;= &lt;= AND ) 组合会被接口的 antlr 预处理拒绝
    ///     ( 2026-09-30 实测 "参数预处理错误" ) , 翻窗口由调用方逐日请求
    /// </summary>
    public const string QueryUrlTemplate =
        "https://datacenter-web.eastmoney.com/api/data/v1/get?reportName={0}&columns=ALL&source=WEB&client=WEB" +
        "&sortColumns={1}&sortTypes=-1&pageSize={4}&pageNumber={5}&filter=({2}%3D%27{3}%27)";

    public const string ResourceHost = "datacenter-web.eastmoney.com";

    /// <summary>
    ///     单页条数 ( 接口上限 500 )
    /// </summary>
    public const int MaxPageSize = 500;

    /// <summary>
    ///     三类榜单的接口配置 ( 北向因停止每日披露暂无 reportName , 类型枚举预留 )
    /// </summary>
    public static readonly IReadOnlyList<RankingReportConfig> Reports =
    [
        new(RankingType.Lhb, "RPT_DAILYBILLBOARD_DETAILSNEW", "BILLBOARD_DEAL_AMT", "TRADE_DATE"),
        new(RankingType.BlockTrade, "RPT_DATA_BLOCKTRADE", "TRADE_DATE", "TRADE_DATE"),
        new(RankingType.Margin, "RPTA_WEB_RZRQ_GGMX", "DATE", "DATE")
    ];

    /// <summary>单个榜单类型的接口配置 ( record 便于加行 )</summary>
    public sealed record RankingReportConfig(RankingType Type, string ReportName, string SortColumn, string DateColumn);
}
