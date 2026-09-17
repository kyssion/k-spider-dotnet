using KSpider.Data;
using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.News;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace KSpider.Job.News;

/// <summary>
///     原始内容下载任务 : 取全部源待下载的新闻 , 按 from_media 分发到对应源爬虫 , 落 spider_news_content_origin
/// </summary>
public class NewsContentOriginJob(SpiderNewsDao spiderNewsDao, Pg pg,
    ILogger<NewsContentOriginJob> logger) : SpiderJob
{
    // 直接通过 newslist db 拉取数据， 落 origin db 中
    public async Task SyncContentOriginInfoByBatch(int pageSize)
    {
        using var connection = pg.Connection();
        // 待下载 = 从未下载 (0) + 下载失败但未达重试上限 (4) , 全源按 Id 先进先出防止积压时老新闻饥饿
        var newsListInfos = connection.Queryable<SpiderNewsListModel>()
            .Where(it => it.DownloadStatusCode == (int)NewsDownloadStatusCode.NoDownload ||
                         (it.DownloadStatusCode == (int)NewsDownloadStatusCode.FailedDownloadOriginInfo &&
                          it.FailCount < NewsPipelineConst.MaxFailCount))
            .OrderBy(item => item.Id, OrderByType.Asc).Take(pageSize).ToList();
        foreach (var newsItem in newsListInfos)
        {
            // 源未注册属于配置错误 , 跳过且不消耗重试次数
            var spider = NewsSpiderRegistry.Get(newsItem.FromMedia ?? 0);
            if (spider == null)
            {
                logger.LogError("[SyncContentOriginInfoByBatch] source not registered , from_media : {} , url : {}",
                    newsItem.FromMedia, newsItem.NewsUrl);
                continue;
            }

            try
            {
                var contentOrigin = await spider.GetContentOrigin(newsItem);
                newsItem.DownloadStatusCode = contentOrigin.Status == NewsContentOriginStatus.Success
                    ? (int)NewsDownloadStatusCode.SuccessDownloadOriginInfo
                    : MarkFailed(newsItem);
                connection.Ado.BeginTran();
                spiderNewsDao.UpsetSpiderNewsContentOrigin(connection, contentOrigin.ToModel());
                spiderNewsDao.UpdateSpiderNewListDownloadStatus(connection, newsItem);
                connection.Ado.CommitTran();
            }
            catch (KDbException e)
            {
                connection.Ado.RollbackTran();
                logger.LogError("[SyncContentOriginInfoByBatch] db err : {} , url : {}", e, newsItem.NewsUrl);
            }
            catch (Exception e)
            {
                connection.Ado.RollbackTran();
                logger.LogError("[SyncContentOriginInfoByBatch] err : {} , url : {}", e, newsItem.NewsUrl);
                MarkFailed(newsItem);
                spiderNewsDao.UpdateSpiderNewListDownloadStatus(connection, newsItem);
            }
        }
    }

    /// <summary>
    ///     记录一次失败 : 状态置为下载失败并累计重试次数
    /// </summary>
    private static int MarkFailed(SpiderNewsListModel newsItem)
    {
        newsItem.DownloadStatusCode = (int)NewsDownloadStatusCode.FailedDownloadOriginInfo;
        newsItem.FailCount += 1;
        return newsItem.DownloadStatusCode;
    }

    public override Task Execute(IJobExecutionContext context)
    {
        return SyncContentOriginInfoByBatch(200);
    }
}
