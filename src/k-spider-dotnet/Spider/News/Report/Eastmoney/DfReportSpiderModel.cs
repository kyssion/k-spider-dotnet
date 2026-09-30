using System.Globalization;
using System.Text.Json.Nodes;
using KSpider.Model;
using KSpider.Spider;

namespace KSpider.Spider.News.Report.Eastmoney;

/// <summary>
///     研报类型 : 对应列表接口 qType ( 枚举值 = qType + 1 , 使 0 不作为有效值 )
/// </summary>
public enum ResearchReportKind
{
    /// <summary>个股研报 ( qType=0 ) : 有个股代码/名称、目标价与盈利预测</summary>
    Stock = 1,

    /// <summary>行业研报 ( qType=1 ) : 有行业名 , 无个股维度</summary>
    Industry = 2,

    /// <summary>宏观研究 ( qType=2 ) : 评级/个股/行业均空</summary>
    Macro = 3
}

/// <summary>
///     研报列表页 : 条目 + 分页信息 ( 页码翻页语义 , TotalPage 来自接口 )
/// </summary>
public sealed record DfReportListPage(List<SpiderResearchReportModel> Items, int TotalPage)
{
    /// <summary>是否还有下一页</summary>
    public bool HasNextPage(int pageNo) => pageNo < TotalPage;
}

/// <summary>
///     研报列表条目解析 : reportapi /report/list 的 data[] 项 → 落库模型。
///     三类条目同构 ( 字段全集一致 ) , 差异只在填充 : 个股有 stock/目标价/预测 ,
///     行业有 industryName , 宏观大多为空 ; 空串一律归一为 null。
/// </summary>
public static class DfReportListItem
{
    /// <summary>
    ///     解析单个条目 ; infoCode / title / publishDate 缺失视为坏数据返回 null ( 跳过 ,
    ///     与快讯源 "id 缺失跳过" 同款约定 )
    /// </summary>
    public static SpiderResearchReportModel? FromJson(JsonNode node, ResearchReportKind kind)
    {
        var infoCode = Text(node, "infoCode");
        var title = Text(node, "title");
        var publishDate = Text(node, "publishDate");
        if (string.IsNullOrEmpty(infoCode) || string.IsNullOrEmpty(title) ||
            !DateTime.TryParse(publishDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return null;

        return new SpiderResearchReportModel
        {
            FromMedia = (int)FromTypeOfNews.DfMedia,
            ReportKind = (short)kind,
            InfoCode = infoCode,
            Title = title,
            StockCode = Text(node, "stockCode"),
            StockName = Text(node, "stockName"),
            OrgName = Text(node, "orgSName") ?? Text(node, "orgName"),
            RatingName = Text(node, "emRatingName"),
            // 个股研报填细分行业 ( indvInduName ) , 行业研报填所属行业 ( industryName ) , 两者互斥
            IndustryName = Text(node, "indvInduName") ?? Text(node, "industryName"),
            AimPriceHigh = Dec(node, "indvAimPriceT"),
            AimPriceLow = Dec(node, "indvAimPriceL"),
            EpsThisYear = Dec(node, "predictThisYearEps"),
            PeThisYear = Dec(node, "predictThisYearPe"),
            EpsNextYear = Dec(node, "predictNextYearEps"),
            PeNextYear = Dec(node, "predictNextYearPe"),
            Researcher = Text(node, "researcher"),
            // 接口发布时间只到日期级 ( 恒为 00:00:00 ) , Npgsql 传参统一 Unspecified ( 避免 timestamptz 误编 )
            PublishDate = DateTime.SpecifyKind(date, DateTimeKind.Unspecified),
            RawContent = node.ToJsonString()
        };
    }

    /// <summary>字符串字段 : 缺失或空串归一为 null</summary>
    private static string? Text(JsonNode node, string key)
    {
        var value = node[key]?.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>数值字段 : 空串 ( 行业/宏观无预测很常见 ) 或非数值归一为 null</summary>
    private static decimal? Dec(JsonNode node, string key)
    {
        var value = node[key]?.ToString();
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;
    }
}
