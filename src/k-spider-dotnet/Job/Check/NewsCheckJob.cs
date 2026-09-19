using KSpider.Data;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.News;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace KSpider.Job.Check;

/// <summary>
///     爬虫健康检查任务 : 1. 各源栏目列表接口可用性探测 2. 新闻流水线积压/失败统计
/// </summary>
public class NewsCheckJob(Pg pg, ILogger<NewsCheckJob> logger) : SpiderJob
{
    public override Task Execute(IJobExecutionContext context)
    {
        return Task.Run(async () =>
        {
            await CheckListApiAlive();
            CheckPipelineBacklog();
        });
    }

    /// <summary>
    ///     逐个源的栏目探测列表接口是否仍返回有效数据 ( 防止源改版后静默失效 )
    /// </summary>
    private async Task CheckListApiAlive()
    {
        foreach (var spider in NewsSpiderRegistry.All)
        foreach (var column in spider.Columns)
            try
            {
                var listPage = await spider.GetListPage(column, 10, null);
                if (listPage.Items.Count == 0) throw new Exception("接口未返回数据");
            }
            catch (Exception e)
            {
                logger.LogError("[NewsCheckJob CheckListApiAlive] api err : {} , source : {} , column : {}", e,
                    spider.FromMedia, column.ColumnId);
            }
    }

    /// <summary>
    ///     按源统计新闻流水线各状态的数量与最老待处理新闻的滞留时长
    /// </summary>
    private void CheckPipelineBacklog()
    {
        try
        {
            using var connection = pg.Connection();
            foreach (var spider in NewsSpiderRegistry.All)
            {
                var fromMedia = (int)spider.FromMedia;
                var statusCounts = connection.Queryable<SpiderNewsListModel>()
                    .Where(it => it.FromMedia == fromMedia)
                    .GroupBy(it => it.DownloadStatusCode)
                    .Select(it => new
                    {
                        it.DownloadStatusCode,
                        Count = SqlFunc.AggregateCount(it.Id)
                    }).ToList();
                var statusText = string.Join(" , ",
                    statusCounts.Select(it => $"{(NewsDownloadStatusCode)it.DownloadStatusCode}={it.Count}"));
                logger.LogInformation("[NewsCheckJob CheckPipelineBacklog] source : {Source} , status : {Status}",
                    spider.FromMedia, statusText);
            }

            var oldestPending = connection.Queryable<SpiderNewsListModel>()
                .Where(it => it.DownloadStatusCode == (int)NewsDownloadStatusCode.NoDownload)
                .Min(it => it.NewsTime);
            logger.LogInformation("[NewsCheckJob CheckPipelineBacklog] oldest pending news time : {Oldest}",
                oldestPending);
        }
        catch (Exception e)
        {
            logger.LogError("[NewsCheckJob CheckPipelineBacklog] err : {}", e);
        }
    }
}
