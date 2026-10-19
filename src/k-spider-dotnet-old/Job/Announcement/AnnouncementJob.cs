using KSpider.Data;
using KSpider.Exceptions;
using KSpider.Spider.Announcement.Cninfo;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace KSpider.Job.Announcement;

/// <summary>
///     公告任务 : 巨潮资讯沪深京全市场法定披露 ( 分类白名单 ) ,
///     直写 spider_announcement ( ON CONFLICT DO NOTHING , 披露即终态 )。
///     翻页停止判定 : 当前页公告键全部已存在即停 ( 列表时间从新到旧 , 首页全旧 = 后面更旧 ) ;
///     公告时效最强 , 10 分钟一轮。
/// </summary>
public class AnnouncementJob(SpiderAnnouncementDao announcementDao, Pg pg, ILogger<AnnouncementJob> logger)
    : SpiderJob
{
    /// <summary>
    ///     每轮最多翻页数 : 白名单约 500 条/日 ÷ 30 条/页 ≈ 17 页/日 ,
    ///     3 天窗口为保护上限 ( 平时首页全旧即停 )
    /// </summary>
    private const int MaxPagePerRound = 40;

    /// <summary>拉取窗口 ( 自然日 ) : 覆盖周末</summary>
    private const int WindowDays = 3;

    private readonly CninfoAnnouncementSpider _spider = new();

    public override async Task Execute(IJobExecutionContext context)
    {
        var endDate = DateTime.Today;
        var beginDate = endDate.AddDays(-WindowDays);
        var inserted = 0;
        try
        {
            inserted = await FetchAsync(beginDate, endDate);
        }
        catch (Exception e)
        {
            logger.LogError("[AnnouncementJob Execute] err : {}", e);
        }

        RunSummary = $"公告新增 {inserted}";
        logger.LogInformation("[AnnouncementJob Execute] run success , insert {}", inserted);
    }

    /// <summary>
    ///     分页拉取窗口公告 , 每页 upsert 后检查存在性 ,
    ///     当前页键全部已存在即停 ( 或 hasMore=false / 到达页数上限 )
    /// </summary>
    private async Task<int> FetchAsync(DateTime beginDate, DateTime endDate)
    {
        var inserted = 0;
        using var connection = pg.Connection();
        for (var pageNo = 1; pageNo <= MaxPagePerRound; pageNo++)
        {
            var page = await _spider.GetAnnouncementPage(beginDate, endDate, pageNo);
            if (page.Items.Count == 0) break;

            inserted += announcementDao.UpsertAnnouncementOnConflict(connection, page.Items, 200);

            var existsIds = announcementDao.GetExistsIds(connection,
                page.Items.Select(item => item.AnnouncementId).ToList());
            if (page.Items.All(item => existsIds.Contains(item.AnnouncementId))) break;
            if (!page.HasMore) break;
        }

        return inserted;
    }
}
