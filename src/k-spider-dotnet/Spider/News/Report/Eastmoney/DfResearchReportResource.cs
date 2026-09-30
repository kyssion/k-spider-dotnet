namespace KSpider.Spider.News.Report.Eastmoney;

/// <summary>
///     东财研报源常量 ( 研报中心 reportapi , 与新闻管线的 np-listapi 不是同一套接口 )。
///     参数约定来自被删占位骨架 ( DfStockResearchReportResource ) + 2026-09-30 实测 :
///     pageSize=100 正常工作 ( 行业 135 条命中分 2 页 ) , 200 不报错返回全部命中 , 统一钳到 100 ;
///     无签名 / 无专用请求头 , 匿名可访问。
/// </summary>
public static class DfResearchReportResource
{
    public const string ParserCode = "df-report-v1";

    public const string ListUrl = "https://reportapi.eastmoney.com/report/list";

    public const string ListResourceHost = "reportapi.eastmoney.com";

    /// <summary>
    ///     详情页 ( 摘要正文 ) 模板 , {0} 为 infocode ; 三类研报的模板路径不同 ( 2026-09-30 实测全部有效 )
    /// </summary>
    public const string StockDetailUrlTemplate = "https://data.eastmoney.com/report/zw_stock.jshtml?infocode={0}";

    public const string IndustryDetailUrlTemplate = "https://data.eastmoney.com/report/zw_industry.jshtml?infocode={0}";

    public const string MacroDetailUrlTemplate = "https://data.eastmoney.com/report/zw_macresearch.jshtml?infocode={0}";

    public const string DetailResourceHost = "data.eastmoney.com";

    /// <summary>
    ///     单页条数上限 ( 实测 100 正常 , 统一钳制防参数越界 )
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    ///     每轮列表拉取的时间窗 ( 天 ) : 研报按发布日集中 ( 盘后/早间 ) ,
    ///     三天窗覆盖周报/晨会的发布日与实际发布时间错位 , 以及短时停机回补
    /// </summary>
    public const int QueryWindowDays = 3;
}
