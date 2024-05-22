using k_spider_dotnet.dal.db;
using k_spider_dotnet.dao;
using k_spider_dotnet.exception;
using k_spider_dotnet.model;
using k_spider_dotnet.script;
using k_spider_dotnet.script.df_news.spider;
using k_spider_dotnet.tool.log;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace k_spider_dotnet.job.dfNewsJob;

public class DfNewsContentOriginJob : SpiderJob
{
    private const string JobName = "DfNewsContentOriginJob";
    private const string JobDescription = "东方财富网站抓取新闻详情原始数据任务";

    private const string FailDownloadOriginString =
        "{\"data\":null,\"errorcode\":0,\"id\":\"-1\",\"message\":\"未获取到文章信息..\",\"success\":0}";

    private static readonly ILogger Logger = LogFactory.GetLogger<DfNewsContentJob>();

    // 直接通过newslist db 拉取数据， 落origin db 中
    public void SyncDfContentInfoOriginInfoByBatch(int pageSize, List<int> newsStatus)
    {
        using var connection = Pg.Connection();
        var newsListInfos = connection.Queryable<SpiderNewsListModel>()
            .Where(it => newsStatus.Contains(it.DownloadStatusCode) && it.FromMedia == (int)FromTypeOfNews.DfMedia)
            .OrderBy(item => item.NewsTime, OrderByType.Desc).Take(pageSize).ToList();
        var spiderContextOrigin = new DfContentSpider();
        foreach (var newsItem in newsListInfos)
        {
            var originHistory = connection.Queryable<SpiderNewsContentOriginModel>()
                .Where(it => it.NewsUrl == newsItem.NewsUrl).First();
            if (originHistory != null && originHistory.NewsOriginContent != FailDownloadOriginString) continue;

            try
            {
                var dfNewsContentOrigin =
                    spiderContextOrigin.GetDfContentOriginInfoByInterface(newsItem.NewsUrl ?? "").Result;
                if (dfNewsContentOrigin.Status == NewsContentOriginStatus.Success)
                    newsItem.DownloadStatusCode = (int)NewsDownloadStatusCode.SuccessDownloadOriginInfo;
                else
                    newsItem.DownloadStatusCode = (int)NewsDownloadStatusCode.FailedDownloadOriginInfo;
                connection.Ado.BeginTran();
                SpiderNewsDao.UpsetSpiderNewsContentOrigin(connection,
                    dfNewsContentOrigin.ToSpiderNewsContentOriginModel());
                SpiderNewsDao.UpdateSpiderNewListDownloadStatus(connection, newsItem);
                connection.Ado.CommitTran();
            }
            catch (KDbException e)
            {
                Logger.LogError("[SyncDfContentInfoOriginInfoByBatch] db err : {}", e);
            }
            catch (Exception e)
            {
                Logger.LogError("[SyncDfContentInfoOriginInfoByBatch] err : {}", e);
                newsItem.DownloadStatusCode = (int)NewsDownloadStatusCode.FailedDownloadOriginInfo;
                SpiderNewsDao.UpdateSpiderNewListDownloadStatus(connection, newsItem);
            }
            finally
            {
                connection.Ado.RollbackTran();
            }
        }
    }

    public override Task Execute(IJobExecutionContext context)
    {
        return Task.Run(() =>
        {
            SyncDfContentInfoOriginInfoByBatch(5000,
            [
                (int)NewsDownloadStatusCode.NoDownload
            ]);
        });
    }

    public override ITrigger GetTrigger(string jobGroup, IJobDetail jobDetail)
    {
        return TriggerBuilder.Create().ForJob(jobDetail)
            .WithIdentity(JobName + ".Trigger", jobGroup + ".Trigger").StartNow()
            .WithSimpleSchedule(x => x.WithIntervalInMinutes(1).RepeatForever().Build())
            .Build();
    }

    public override IJobDetail GetJobDetail(string jobGroup)
    {
        return JobBuilder.Create<DfNewsContentOriginJob>().WithIdentity(JobName + ".Job", jobGroup + ".Job")
            .DisallowConcurrentExecution() // 禁止并发执行
            .WithDescription(JobDescription).Build();
    }
}