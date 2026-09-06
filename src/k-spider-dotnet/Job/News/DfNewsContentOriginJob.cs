using KSpider.Data;
using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.DfNews;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace KSpider.Job.News;

public class DfNewsContentOriginJob(SpiderNewsDao spiderNewsDao, Pg pg,
    ILogger<DfNewsContentOriginJob> logger) : SpiderJob
{
    // 直接通过newslist db 拉去数据， 落origin db 中
    public async Task SyncDfContentInfoOriginInfoByBatch(int pageSize)
    {
        using var connection = pg.Connection();
        // 待下载 = 从未下载 (0) + 下载失败但未达重试上限 (4) , 按 Id 先进先出防止积压时老新闻饥饿
        var newsListInfos = connection.Queryable<SpiderNewsListModel>()
            .Where(it => (it.DownloadStatusCode == (int)NewsDownloadStatusCode.NoDownload ||
                          (it.DownloadStatusCode == (int)NewsDownloadStatusCode.FailedDownloadOriginInfo &&
                           it.FailCount < NewsPipelineConst.MaxFailCount)) &&
                         it.FromMedia == (int)FromTypeOfNews.DfMedia)
            .OrderBy(item => item.Id, OrderByType.Asc).Take(pageSize).ToList();
        var spiderContextOrigin = new DfContentSpider();
        foreach (var newsItem in newsListInfos)
        {
            try
            {
                var dfNewsContentOrigin =
                    await spiderContextOrigin.GetDfContentOriginInfoByInterface(newsItem.NewsUrl ?? "");
                newsItem.DownloadStatusCode = dfNewsContentOrigin.Status == NewsContentOriginStatus.Success
                    ? (int)NewsDownloadStatusCode.SuccessDownloadOriginInfo
                    : MarkFailed(newsItem);
                connection.Ado.BeginTran();
                spiderNewsDao.UpsetSpiderNewsContentOrigin(connection,
                    dfNewsContentOrigin.ToSpiderNewsContentOriginModel());
                spiderNewsDao.UpdateSpiderNewListDownloadStatus(connection, newsItem);
                connection.Ado.CommitTran();
            }
            catch (KDbException e)
            {
                connection.Ado.RollbackTran();
                logger.LogError("[SyncDfContentInfoOriginInfoByBatch] db err : {} , url : {}", e, newsItem.NewsUrl);
            }
            catch (Exception e)
            {
                connection.Ado.RollbackTran();
                logger.LogError("[SyncDfContentInfoOriginInfoByBatch] err : {} , url : {}", e, newsItem.NewsUrl);
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
        return SyncDfContentInfoOriginInfoByBatch(200);
    }
}
