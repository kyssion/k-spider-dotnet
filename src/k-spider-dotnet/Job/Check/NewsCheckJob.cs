using KSpider.Data;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.FlashNews;
using KSpider.Spider.News;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace KSpider.Job.Check;

/// <summary>
///     爬虫健康检查任务 :
///     1. 各源栏目接口可用性探测 ( 网页型 + 快讯型 )
///     2. 网页型流水线积压 / 失败统计
///     3. 快讯源实时性滞后监控 ( 最新一条距现在多久 )
/// </summary>
public class NewsCheckJob(Pg pg, ILogger<NewsCheckJob> logger) : SpiderJob
{
    public override Task Execute(IJobExecutionContext context)
    {
        return Task.Run(async () =>
        {
            await CheckListApiAlive();
            CheckPipelineBacklog();
            CheckFlashNewsLag();
        });
    }

    /// <summary>
    ///     逐个源的栏目探测接口是否仍返回有效数据 ( 防止源改版后静默失效 )
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

        foreach (var spider in FlashNewsSpiderRegistry.All)
        foreach (var column in spider.Columns)
            try
            {
                var page = await spider.GetFlashPage(column, 10, null);
                if (page.Items.Count == 0) throw new Exception("接口未返回数据");
            }
            catch (Exception e)
            {
                logger.LogError("[NewsCheckJob CheckListApiAlive] api err : {} , source : {} , column : {}", e,
                    spider.FromMedia, column.ColumnId);
            }
    }

    /// <summary>
    ///     网页型流水线 : 按源统计各状态的数量与最老待处理新闻的滞留时长
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

    /// <summary>
    ///     快讯源实时性 : 最新一条距现在多久。快讯的价值在实时 ,
    ///     滞后超过两轮轮询 ( 30 秒 ) 说明该源接口异常或长时间无发布 , 需要关注。
    /// </summary>
    private void CheckFlashNewsLag()
    {
        try
        {
            using var connection = pg.Connection();
            foreach (var spider in FlashNewsSpiderRegistry.All)
            {
                var fromMedia = (int)spider.FromMedia;
                var newest = connection.Queryable<SpiderFlashNewsModel>()
                    .Where(it => it.FromMedia == fromMedia)
                    .Max(it => it.NewsTime);
                if (newest == default)
                {
                    logger.LogWarning("[NewsCheckJob CheckFlashNewsLag] source : {Source} , no data yet",
                        spider.FromMedia);
                    continue;
                }

                var lagSeconds = (int)(DateTime.Now - newest).TotalSeconds;
                // 工作时段 ( 9~23 点 ) 快讯频繁 , 滞后大基本等于异常 ; 深夜本来就少 , 阈值放宽
                var threshold = newest.Hour is >= 9 and < 23 ? 300 : 3600;
                var level = lagSeconds > threshold ? LogLevel.Warning : LogLevel.Information;
                logger.Log(level,
                    "[NewsCheckJob CheckFlashNewsLag] source : {Source} , newest : {Newest:yyyy-MM-dd HH:mm:ss} , lag : {Lag}s",
                    spider.FromMedia, newest, lagSeconds);
            }
        }
        catch (Exception e)
        {
            logger.LogError("[NewsCheckJob CheckFlashNewsLag] err : {}", e);
        }
    }
}
