using KSpider.Data;
using KSpider.Exceptions;
using KSpider.Spider.Ranking.Eastmoney;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace KSpider.Job.Ranking;

/// <summary>
///     盘面榜单任务 : 东财数据中心 reportName 接口族 ( 龙虎榜/大宗/两融 ) ,
///     每轮拉近几天窗口逐类拉全 , 直写 spider_ranking ( ON CONFLICT DO NOTHING , 发布即终态 )。
///     翻页停止判定 : 当前页行键全部已存在即停 ( 数据按日期从新到旧排序 , 首页全旧 = 后面更旧 )。
///     北向资金因交易所停止每日披露暂无数据源 ( 契约预留 , 见 docs/architecture.md )。
/// </summary>
public class RankingJob(SpiderRankingDao rankingDao, Pg pg, ILogger<RankingJob> logger) : SpiderJob
{
    /// <summary>
    ///     每类单日最多翻页数 : 两融全市场约 7 页 ( 500/页 ) 是最大类 , 其余类 1-2 页 ; 保护性上限
    /// </summary>
    private const int MaxPagePerDay = 40;

    private const int PageSize = 500;

    /// <summary>拉取窗口 ( 自然日 ) : 覆盖 T+1 披露 ( 两融/大宗 ) 与周末</summary>
    private const int WindowDays = 4;

    private readonly DfRankingSpider _spider = new();

    public override async Task Execute(IJobExecutionContext context)
    {
        var endDate = DateTime.Today;
        var beginDate = endDate.AddDays(-WindowDays);
        var inserted = 0;
        foreach (var config in DfRankingResource.Reports)
            try
            {
                inserted += await FetchTypeAsync(config, beginDate, endDate);
            }
            catch (Exception e)
            {
                logger.LogError("[RankingJob Execute] type : {} , err : {}", config.Type, e);
            }

        RunSummary = $"榜单新增 {inserted}";
        logger.LogInformation("[RankingJob Execute] run success , insert {}", inserted);
    }

    /// <summary>
    ///     拉取单类榜单 : 逐日拉取 ( filter 单日语义 ) + 页码翻页 ,
    ///     单日单类失败不影响其它日与其它类
    /// </summary>
    private async Task<int> FetchTypeAsync(DfRankingResource.RankingReportConfig config,
        DateTime beginDate, DateTime endDate)
    {
        var inserted = 0;
        using var connection = pg.Connection();
        for (var day = beginDate; day <= endDate; day = day.AddDays(1))
            try
            {
                inserted += await FetchDayAsync(connection, config, day);
            }
            catch (Exception e)
            {
                logger.LogError("[RankingJob FetchTypeAsync] type : {} , date : {} , err : {}",
                    config.Type, day.ToString("yyyy-MM-dd"), e);
            }

        return inserted;
    }

    /// <summary>
    ///     拉取单类单日 : 页码翻页 , 每页 upsert 后检查行键存在性 ,
    ///     当前页行键全部已存在即停 ( 后面页只会在更旧 , 已无新数据 )
    /// </summary>
    private async Task<int> FetchDayAsync(SqlSugarClient connection,
        DfRankingResource.RankingReportConfig config, DateTime day)
    {
        var inserted = 0;
        for (var pageNo = 1; pageNo <= MaxPagePerDay; pageNo++)
        {
            var page = await _spider.GetReportPage(config, day, day, PageSize, pageNo);
            if (page.Items.Count == 0) break;

            inserted += rankingDao.UpsertRankingOnConflict(connection, page.Items, 200);

            var existsKeys = rankingDao.GetExistsRowKeys(connection, (short)config.Type, day,
                page.Items.Select(item => item.RowKey).ToList());
            if (page.Items.All(item => existsKeys.Contains(item.RowKey))) break;
            if (!page.HasNextPage(pageNo)) break;
        }

        return inserted;
    }
}
