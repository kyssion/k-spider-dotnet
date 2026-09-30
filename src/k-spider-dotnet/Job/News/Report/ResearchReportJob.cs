using KSpider.Data;
using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider.News.Report.Eastmoney;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace KSpider.Job.News.Report;

/// <summary>
///     研报任务 : 东财研报中心三类研报 ( 个股/行业/宏观 ) , 列表即结构化元数据 ,
///     直写 spider_research_report ( ON CONFLICT DO NOTHING ) ;
///     摘要正文由详情页二段回填 ( summary 为空且失败未达上限的行 , 每轮限量 )。
///     与快讯型同款约定 : 拉取失败记日志 , 下一轮 ( 5 分钟后 ) 自然重试。
/// </summary>
public class ResearchReportJob(SpiderResearchReportDao reportDao, Pg pg, ILogger<ResearchReportJob> logger)
    : SpiderJob
{
    /// <summary>
    ///     每类每轮最多翻页数 : 三天窗口通常一页 ( ≤100 条 ) 装得下 ,
    ///     研报密集发布日 ( 财报季 ) 按此向更早翻
    /// </summary>
    private const int MaxPageNumber = 4;

    private const int PageSize = 100;

    /// <summary>每轮摘要回填条数上限 : 20 次详情请求 , 5 分钟一轮足以消化日常增量</summary>
    private const int SummaryBatchSize = 20;

    private readonly DfResearchReportSpider _spider = new();

    public override async Task Execute(IJobExecutionContext context)
    {
        var insertNumber = await FetchListAsync();
        var summaryFilled = await BackfillSummariesAsync();
        RunSummary = $"新增研报 {insertNumber} , 回填摘要 {summaryFilled}";
        logger.LogInformation("[ResearchReportJob Execute] run success , insert {} , summary filled {}",
            insertNumber, summaryFilled);
    }

    /// <summary>
    ///     三类研报逐类拉取近几天窗口 ( 单类失败不影响其它类 ) ,
    ///     窗口条目每轮重拉 , 已存在行由 ON CONFLICT DO NOTHING 吸收
    /// </summary>
    private async Task<int> FetchListAsync()
    {
        var endDate = DateTime.Today;
        var beginDate = endDate.AddDays(-DfResearchReportResource.QueryWindowDays);
        var insertNumber = 0;
        using var connection = pg.Connection();
        foreach (var kind in DfResearchReportSpider.Kinds)
            try
            {
                insertNumber += await FetchKindAsync(connection, kind, beginDate, endDate);
            }
            catch (Exception e)
            {
                logger.LogError("[ResearchReportJob FetchListAsync] kind : {} , err : {}", kind, e);
            }

        return insertNumber;
    }

    /// <summary>
    ///     拉取单类研报 : 页码翻页 , 满窗按 TotalPage 最多翻 MaxPageNumber 页 ( 停机回补上限 )
    /// </summary>
    private async Task<int> FetchKindAsync(SqlSugarClient connection, ResearchReportKind kind,
        DateTime beginDate, DateTime endDate)
    {
        var insertNumber = 0;
        for (var pageNo = 1; pageNo <= MaxPageNumber; pageNo++)
        {
            var page = await _spider.GetReportPage(kind, beginDate, endDate, PageSize, pageNo);
            insertNumber += reportDao.UpsertResearchReportOnConflict(connection, page.Items, 200);
            if (!page.HasNextPage(pageNo)) break;
        }

        return insertNumber;
    }

    /// <summary>
    ///     摘要回填 : 待回填行逐条请求详情页 , 成功写 summary , 失败计数 +1。
    ///     单条失败不中断整批 ; KDbException ( 库不可用 ) 直接中断本轮 —— 计数写不进去 ,
    ///     下一轮自然重试且不消耗重试次数 ( 与主管线"数据库异常不消耗 fail_count"同语义 )。
    /// </summary>
    private async Task<int> BackfillSummariesAsync()
    {
        using var connection = pg.Connection();
        List<SpiderResearchReportModel> pending;
        try
        {
            pending = reportDao.GetPendingSummaryReports(connection, SummaryBatchSize);
        }
        catch (KDbException e)
        {
            logger.LogError("[ResearchReportJob BackfillSummariesAsync] 查询待回填失败 : {}", e);
            return 0;
        }

        var filled = 0;
        foreach (var report in pending)
            try
            {
                var summary = await _spider.GetReportSummaryAsync(report.InfoCode,
                    (ResearchReportKind)report.ReportKind);
                reportDao.FillSummary(connection, report.Id, summary);
                filled++;
            }
            catch (KDbException e)
            {
                logger.LogError("[ResearchReportJob BackfillSummariesAsync] 回填写库失败 , infoCode : {} , err : {}",
                    report.InfoCode, e);
                return filled;
            }
            catch (Exception e)
            {
                logger.LogWarning("[ResearchReportJob BackfillSummariesAsync] 摘要回填失败 , infoCode : {} , err : {}",
                    report.InfoCode, e);
                reportDao.IncreaseSummaryFailCount(connection, report.Id);
            }

        return filled;
    }
}
