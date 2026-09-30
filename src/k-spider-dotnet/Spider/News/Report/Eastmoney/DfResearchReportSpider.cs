using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider.Verify;
using KSpider.Common;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.News.Report.Eastmoney;

/// <summary>
///     东财研报源 : 列表接口即全部结构化元数据 ( reportapi ) , 摘要正文由详情页 ( SSR HTML ) 二段补齐。
///     独立于新闻管线 ( 不进 NewsSpiderRegistry ) , 由 ResearchReportJob 直连 ;
///     出现第二个研报源时再抽注册表。
/// </summary>
public class DfResearchReportSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<DfResearchReportSpider>();

    /// <summary>
    ///     全部在管研报类型 ( 三类全接 , 同一套接口只差 qType )
    /// </summary>
    public static readonly IReadOnlyList<ResearchReportKind> Kinds =
        [ResearchReportKind.Stock, ResearchReportKind.Industry, ResearchReportKind.Macro];

    /// <summary>
    ///     拉一页研报列表 ( 时间窗 + 页码翻页 ; 每页一条独立请求 )
    /// </summary>
    public async Task<DfReportListPage> GetReportPage(ResearchReportKind kind, DateTime beginDate, DateTime endDate,
        int pageSize, int pageNo)
    {
        var requestSize = Math.Min(pageSize, DfResearchReportResource.MaxPageSize);
        // 枚举值 = qType + 1 , 见 ResearchReportKind 注释
        var url = $"{DfResearchReportResource.ListUrl}" +
                  $"?pageSize={requestSize}" +
                  $"&beginTime={beginDate:yyyy-MM-dd}&endTime={endDate:yyyy-MM-dd}" +
                  $"&pageNo={pageNo}&fields=&qType={(int)kind - 1}";
        try
        {
            var responseString = await VerifiedHttp.GetStringAsync(DfResearchReportResource.ListResourceHost, url);
            return ParseReportPage(responseString, kind);
        }
        catch (Exception e)
        {
            var message =
                $"[DfResearchReportSpider GetReportPage] 拉取研报列表失败 , kind : {kind} , window : {beginDate:yyyy-MM-dd}~{endDate:yyyy-MM-dd} , pageNo : {pageNo} , err : {e}";
            Log.LogError(message);
            throw new HtmlFormException(url, message, e);
        }
    }

    /// <summary>
    ///     解析列表响应 ( 独立成公开静态方法供离线测试 ) ; data 缺失 / 非数组显式报错 , 不当空页
    /// </summary>
    public static DfReportListPage ParseReportPage(string responseString, ResearchReportKind kind)
    {
        JsonNode? jsonNode;
        try
        {
            jsonNode = JsonNode.Parse(responseString);
        }
        catch (JsonException e)
        {
            throw new HtmlFormException(DfResearchReportResource.ListUrl,
                $"[DfResearchReportSpider ParseReportPage] 响应不是合法 JSON : {e.Message}");
        }

        if (jsonNode == null)
            throw new HtmlFormException(DfResearchReportResource.ListUrl,
                "[DfResearchReportSpider ParseReportPage] 响应为空");
        if (jsonNode["data"] is not JsonArray dataArray)
            throw new HtmlFormException(DfResearchReportResource.ListUrl,
                $"[DfResearchReportSpider ParseReportPage] 响应缺少 data 数组 , 原始 : {responseString[..Math.Min(200, responseString.Length)]}");

        var items = new List<SpiderResearchReportModel>();
        foreach (var node in dataArray)
        {
            if (node == null) continue;
            var item = DfReportListItem.FromJson(node, kind);
            if (item != null) items.Add(item);
        }

        var totalPage = jsonNode["TotalPage"]?.GetValue<int>() ?? 1;
        return new DfReportListPage(items, Math.Max(totalPage, 1));
    }

    /// <summary>
    ///     拉取并解析研报摘要正文 ( 详情页 SSR HTML 的 ctx-content 区 , 纯文本段落 )
    /// </summary>
    public async Task<string> GetReportSummaryAsync(string infoCode, ResearchReportKind kind)
    {
        var url = DetailUrlTemplate(kind).Replace("{0}", infoCode);
        try
        {
            var html = await VerifiedHttp.GetStringAsync(DfResearchReportResource.DetailResourceHost, url);
            return ParseReportSummary(html);
        }
        catch (HtmlFormException)
        {
            // 解析错误已是带上下文的形态 , 直接上抛由任务层计失败次数
            throw;
        }
        catch (Exception e)
        {
            var message =
                $"[DfResearchReportSpider GetReportSummaryAsync] 拉取研报摘要失败 , infoCode : {infoCode} , kind : {kind} , url : {url} , err : {e}";
            Log.LogError(message);
            throw new HtmlFormException(url, message, e);
        }
    }

    /// <summary>
    ///     解析摘要正文 ( 独立成公开静态方法供离线测试 ) :
    ///     取 div.ctx-content 内全部段落文本 , 按段落换行拼接 ; 摘要区缺失 / 无段落显式报错 ( 触发重试计数 )
    /// </summary>
    public static string ParseReportSummary(string html)
    {
        var region = SummaryRegionRegex.Match(html);
        if (!region.Success)
            throw new HtmlFormException(DfResearchReportResource.DetailResourceHost,
                "[DfResearchReportSpider ParseReportSummary] 详情页缺少 ctx-content 摘要区 ( 页面结构可能已改版 )");

        var paragraphs = ParagraphRegex.Matches(region.Groups[1].Value)
            .Select(match => CleanParagraph(match.Groups[1].Value))
            .Where(text => text.Length > 0)
            .ToList();
        if (paragraphs.Count == 0)
            throw new HtmlFormException(DfResearchReportResource.DetailResourceHost,
                "[DfResearchReportSpider ParseReportSummary] 摘要区没有可提取的段落");

        return string.Join("\n", paragraphs);
    }

    private static string DetailUrlTemplate(ResearchReportKind kind)
    {
        return kind switch
        {
            ResearchReportKind.Stock => DfResearchReportResource.StockDetailUrlTemplate,
            ResearchReportKind.Industry => DfResearchReportResource.IndustryDetailUrlTemplate,
            ResearchReportKind.Macro => DfResearchReportResource.MacroDetailUrlTemplate,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    /// <summary>段落清洗 : 剥内嵌标签 → 解码实体 → 压缩空白 ( 全角空格缩进一并去掉 )</summary>
    private static string CleanParagraph(string html)
    {
        var text = TagRegex.Replace(html, "");
        text = System.Net.WebUtility.HtmlDecode(text);
        return WhitespaceRegex.Replace(text, " ").Trim();
    }

    /// <summary>摘要区 : div.ctx-content 整段 ( 内部只有 p 段落 , 非贪婪截到首个关闭标签即为本区 )</summary>
    private static readonly Regex SummaryRegionRegex =
        new(@"<div[^>]*class=""ctx-content""[^>]*>(.*?)</div>", RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex ParagraphRegex = new(@"<p[^>]*>(.*?)</p>", RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex TagRegex = new(@"<[^>]+>", RegexOptions.Compiled);

    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
}
