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
///     详情解析任务 : 取全部源已下载原始内容的新闻 , 按 from_media 分发到对应源爬虫解析并落库
/// </summary>
public class NewsContentJob(SpiderNewsDao spiderNewsDao, Pg pg, ILogger<NewsContentJob> logger) : SpiderJob
{
    // 直接通过 newslist db 拉取数据， 从 origin db 解析结构化内容
    public void SyncContentInfoFromOriginInfo(int pageSize)
    {
        using var connection = pg.Connection();
        // 待解析 = 原始已下载 (3) + 解析失败但未达重试上限 (2)
        var newsListInfos = connection.Queryable<SpiderNewsListModel>()
            .Where(it => it.DownloadStatusCode == (int)NewsDownloadStatusCode.SuccessDownloadOriginInfo ||
                         (it.DownloadStatusCode == (int)NewsDownloadStatusCode.FailedSyncDetailInfo &&
                          it.FailCount < NewsPipelineConst.MaxFailCount))
            .OrderBy(item => item.Id, OrderByType.Asc).Take(pageSize).ToList();

        // 一次性批量加载原始内容 , 避免循环内逐条查询
        var newsUrls = newsListInfos.Select(it => it.NewsUrl).ToList();
        var originMap = connection.Queryable<SpiderNewsContentOriginModel>()
            .Where(it => newsUrls.Contains(it.NewsUrl)).ToList()
            .GroupBy(it => it.NewsUrl).ToDictionary(group => group.Key!, group => group.First());

        foreach (var newsItem in newsListInfos)
        {
            if (string.IsNullOrEmpty(newsItem.NewsUrl)) continue;

            // 源未注册属于配置错误 , 跳过且不消耗重试次数
            var spider = NewsSpiderRegistry.Get(newsItem.FromMedia ?? 0);
            if (spider == null)
            {
                logger.LogError("[SyncContentInfoFromOriginInfo] source not registered , from_media : {} , url : {}",
                    newsItem.FromMedia, newsItem.NewsUrl);
                continue;
            }

            // 原始内容缺失 : 退回下载失败状态 , 由 OriginJob 重拉
            if (!originMap.TryGetValue(newsItem.NewsUrl, out var spiderOriginInfo))
            {
                newsItem.DownloadStatusCode = (int)NewsDownloadStatusCode.FailedDownloadOriginInfo;
                newsItem.FailCount += 1;
                UpdateStatus(connection, spiderNewsDao, newsItem);
                logger.LogWarning("[SyncContentInfoFromOriginInfo] origin info not find , back to download , url : {}",
                    newsItem.NewsUrl);
                continue;
            }

            try
            {
                var parseResult = spider.ParseContent(spiderOriginInfo.NewsOriginContent ?? "",
                    spiderOriginInfo.NewsUrl ?? "");
                // 列表接口返回的摘要比详情接口的标题占位更完整 , 优先回填
                if (!string.IsNullOrEmpty(newsItem.NewsSummary))
                    parseResult.Content.NewsSummary = newsItem.NewsSummary;
                newsItem.DownloadStatusCode = (int)NewsDownloadStatusCode.SuccessSyncDetailInfo;
                newsItem.FailCount = 0;
                connection.Ado.BeginTran();
                spiderNewsDao.UpsetSpiderNewsContent(connection, parseResult.Content);
                spiderNewsDao.UpsetSpiderNewsImageList(connection, parseResult.Images);
                spiderNewsDao.UpdateSpiderNewsListInfo(connection, newsItem);
                connection.Ado.CommitTran();
            }
            catch (Exception e)
            {
                connection.Ado.RollbackTran();
                switch (e)
                {
                    case KDbException:
                        // 数据库异常不消耗重试次数 , 保持状态等待下一轮
                        logger.LogError("[SyncContentInfoFromOriginInfo] content  db err : {} ,  url : {}", e,
                            newsItem.NewsUrl);
                        break;
                    default:
                        logger.LogError("[SyncContentInfoFromOriginInfo] content  parse err : {} ,  url : {}", e,
                            newsItem.NewsUrl);
                        newsItem.DownloadStatusCode = (int)NewsDownloadStatusCode.FailedSyncDetailInfo;
                        newsItem.FailCount += 1;
                        UpdateStatus(connection, spiderNewsDao, newsItem);
                        break;
                }
            }
        }
    }

    private void UpdateStatus(SqlSugarClient connection, SpiderNewsDao spiderNewsDao, SpiderNewsListModel newsItem)
    {
        try
        {
            spiderNewsDao.UpdateSpiderNewListDownloadStatus(connection, newsItem);
        }
        catch (Exception exception)
        {
            logger.LogError("[SyncContentInfoFromOriginInfo] UpdateSpiderNewsListInfo err  : {} ,  url : {}",
                exception, newsItem.NewsUrl);
        }
    }

    public override Task Execute(IJobExecutionContext context)
    {
        return Task.Run(() =>
        {
            SyncContentInfoFromOriginInfo(1000);
        });
    }
}
