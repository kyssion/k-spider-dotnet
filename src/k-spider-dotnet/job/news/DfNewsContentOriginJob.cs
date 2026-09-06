using k_spider_dotnet.job;
using k_spider_dotnet.logger;
using k_spider_dotnet.data;
using k_spider_dotnet.exception;
using k_spider_dotnet.model;
using k_spider_dotnet.spider;
using k_spider_dotnet.spider.df_news;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace k_spider_dotnet.job.news;

public class DfNewsContentOriginJob : SpiderJob
{
    private const string JobName = "DfNewsContentOriginJob";
    private const string JobDescription = "东方财富网站抓取新闻详情原始数据任务";

    private static readonly ILogger Logger = LogFactory.GetLogger<DfNewsContentOriginJob>();

    // 直接通过newslist db 拉去数据， 落origin db 中
    public async Task SyncDfContentInfoOriginInfoByBatch(int pageSize)
    {
        using var connection = Pg.Connection();
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
                SpiderNewsDao.UpsetSpiderNewsContentOrigin(connection,
                    dfNewsContentOrigin.ToSpiderNewsContentOriginModel());
                SpiderNewsDao.UpdateSpiderNewListDownloadStatus(connection, newsItem);
                connection.Ado.CommitTran();
            }
            catch (KDbException e)
            {
                connection.Ado.RollbackTran();
                Logger.LogError("[SyncDfContentInfoOriginInfoByBatch] db err : {} , url : {}", e, newsItem.NewsUrl);
            }
            catch (Exception e)
            {
                connection.Ado.RollbackTran();
                Logger.LogError("[SyncDfContentInfoOriginInfoByBatch] err : {} , url : {}", e, newsItem.NewsUrl);
                MarkFailed(newsItem);
                SpiderNewsDao.UpdateSpiderNewListDownloadStatus(connection, newsItem);
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

    public override ITrigger GetTrigger(string jobGroup, IJobDetail jobDetail)
    {
        return TriggerBuilder.Create().ForJob(jobDetail)
            .WithIdentity(JobName + ".Trigger", jobGroup + ".Trigger").StartNow()
            .WithSimpleSchedule(x => x.WithIntervalInSeconds(3).RepeatForever().Build())
            .Build();
    }

    public override IJobDetail GetJobDetail(string jobGroup)
    {
        return JobBuilder.Create<DfNewsContentOriginJob>().WithIdentity(JobName + ".Job", jobGroup + ".Job")
            .DisallowConcurrentExecution() // 禁止并发执行
            .WithDescription(JobDescription).Build();
    }
}
