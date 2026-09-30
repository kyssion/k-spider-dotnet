using System.Text.Json;
using System.Text.Json.Nodes;
using KSpider.Exceptions;
using KSpider.Spider.Verify;
using KSpider.Common;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.Ranking.Eastmoney;

/// <summary>
///     东财数据中心盘面榜单源 ( Ranking 管线 , reportName 接口族 ) :
///     列表即结构化数值直写 , 无详情无解析二段 ( 行即数据 , 与研报的"摘要回填"也不同 )。
///     独立于新闻注册表 , 由 RankingJob 直连 ; datecenter filter 的日期列名按类型配置。
/// </summary>
public class DfRankingSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<DfRankingSpider>();

    /// <summary>
    ///     拉一页榜单 ( 日期窗 + 页码翻页 , 按类型配置的 reportName 与日期列 )
    /// </summary>
    public async Task<DfRankingPage> GetReportPage(DfRankingResource.RankingReportConfig config,
        DateTime beginDate, DateTime endDate, int pageSize, int pageNo)
    {
        var requestSize = Math.Min(pageSize, DfRankingResource.MaxPageSize);
        // 单日 filter : 调用方逐日请求 ( datecenter 范围 filter 语法不被接受 )
        var url = string.Format(DfRankingResource.QueryUrlTemplate,
            config.ReportName, config.SortColumn, config.DateColumn,
            beginDate.ToString("yyyy-MM-dd"), requestSize, pageNo);
        try
        {
            var responseString = await VerifiedHttp.GetStringAsync(DfRankingResource.ResourceHost, url);
            return ParseReportPage(responseString, config.Type);
        }
        catch (Exception e)
        {
            var message =
                $"[DfRankingSpider GetReportPage] 拉取榜单失败 , type : {config.Type} , window : {beginDate:yyyy-MM-dd}~{endDate:yyyy-MM-dd} , pageNo : {pageNo} , err : {e}";
            Log.LogError(message);
            throw new HtmlFormException(url, message, e);
        }
    }

    /// <summary>
    ///     解析榜单响应 ( 独立成公开静态方法供离线测试 ) ;
    ///     result 缺失显式报错不当空页 ; 空数据 ( success=true 但 data 空 ) 返回空页且 TotalPage=1
    /// </summary>
    public static DfRankingPage ParseReportPage(string responseString, RankingType type)
    {
        JsonNode? jsonNode;
        try
        {
            jsonNode = JsonNode.Parse(responseString);
        }
        catch (JsonException e)
        {
            throw new HtmlFormException(DfRankingResource.ResourceHost,
                $"[DfRankingSpider ParseReportPage] 响应不是合法 JSON : {e.Message}");
        }

        if (jsonNode == null)
            throw new HtmlFormException(DfRankingResource.ResourceHost,
                "[DfRankingSpider ParseReportPage] 响应为空");

        var result = jsonNode["result"];
        // 无数据 ( result=null ) : 该日期没有披露——龙虎榜当日盘中/节假日是常态 ,
        // success=true 或 "返回数据为空" 均视为空页 ; 其余 ( 报表不存在等 ) 显式报错
        if (result == null)
        {
            var success = jsonNode["success"]?.ToString();
            var message = jsonNode["message"]?.ToString() ?? "";
            if (success is "true" or "True" || message.Contains("为空"))
                return new DfRankingPage([], 1);
            throw new HtmlFormException(DfRankingResource.ResourceHost,
                $"[DfRankingSpider ParseReportPage] 接口返回错误 : {message}");
        }

        if (result["data"] is not JsonArray dataArray)
            throw new HtmlFormException(DfRankingResource.ResourceHost,
                "[DfRankingSpider ParseReportPage] 响应缺少 result.data 数组");

        var items = new List<KSpider.Model.SpiderRankingModel>();
        foreach (var node in dataArray)
        {
            if (node == null) continue;
            var item = DfRankingItem.FromJson(node, type);
            if (item != null) items.Add(item);
        }

        // 行键去重 : 同日同业务键同数值指纹的完全重复行只留一条 ( 大宗分手成交实测存在 )
        items = items.GroupBy(item => item.RowKey).Select(group => group.First()).ToList();

        var totalPage = result["pages"]?.GetValue<int?>() ?? 1;
        return new DfRankingPage(items, Math.Max(totalPage, 1));
    }
}
