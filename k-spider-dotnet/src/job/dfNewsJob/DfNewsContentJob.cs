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

public class DfNewsContentJob : SpiderJob
{
    private const string JobName = "DfNewsContentJob";
    private const string JobDescription = "东方财富网站抓取新闻详情任务";

    private static readonly ILogger Logger = LogFactory.GetLogger<DfNewsContentJob>();

    // 直接通过newslist db 拉去数据， 不落 origin db 中
    public void SyncDfContentInfoFromOriginInfo(int pageSize, List<int> newsStatus)
    {
        using var connection = Pg.Connection();
        var newsListInfos = connection.Queryable<SpiderNewsListModel>()
            .Where(it => newsStatus.Contains(it.DownloadStatusCode) && it.FromMedia == (int)FromTypeOfNews.DfMedia)
            .OrderBy(item => item.NewsTime, OrderByType.Desc).Take(pageSize).ToList();
        var spiderContent = new DfContentSpider();
        foreach (var newsItem in newsListInfos)
            try
            {
                if (newsItem.NewsUrl == "") continue;

                var spiderOriginInfo = connection.Queryable<SpiderNewsContentOriginModel>()
                    .Where(it => it.NewsUrl == newsItem.NewsUrl).First();

                var dfContentInfo = spiderContent.GetContentInfoByJson(spiderOriginInfo.NewsOriginContent ?? "",
                    spiderOriginInfo.NewsUrl ?? "");
                var imgDbList = dfContentInfo.ImgInfos.Select(item => new SpiderNewsImageListModel
                        { NewsUrl = item.NewsUrl, ImageResourceUrl = item.ResourceUrl, ImageName = item.ImgName })
                    .ToList();
                newsItem.DownloadStatusCode = (int)NewsDownloadStatusCode.SuccessSyncDetailInfo;
                connection.Ado.BeginTran();
                SpiderNewsDao.UpsetSpiderNewsContent(connection, dfContentInfo.ToSpiderNewsContentModel());
                SpiderNewsDao.UpsetSpiderNewsImageList(connection, imgDbList);
                SpiderNewsDao.UpdateSpiderNewsListInfo(connection, newsItem);
                connection.Ado.CommitTran();
            }
            catch (Exception e)
            {
                var needUpdateDb = false;
                switch (e)
                {
                    case DownloadHttpRequestException:
                        Logger.LogError("[DfNewsContentJob] download  err  : {} , url : {} ", e, newsItem.NewsUrl);
                        newsItem.DownloadStatusCode = (int)NewsDownloadStatusCode.FailedSyncDetailInfo;
                        needUpdateDb = true;
                        break;
                    case HtmlFormException:
                        Logger.LogError("[DfNewsContentJob] content  html form err : {} ,  url : {}", e,
                            newsItem.NewsUrl);
                        newsItem.DownloadStatusCode = (int)NewsDownloadStatusCode.FailedSyncDetailInfo;
                        needUpdateDb = true;
                        break;
                    case KDbException:
                        Logger.LogError("[DfNewsContentJob] content  db err : {} ,  url : {}", e, newsItem.NewsUrl);
                        break;
                    default:
                        Logger.LogError("[DfNewsContentJob] content  unknow other err : {} ,  url : {}", e,
                            newsItem.NewsUrl);
                        newsItem.DownloadStatusCode = (int)NewsDownloadStatusCode.FailedSyncDetailInfo;
                        break;
                }

                if (needUpdateDb)
                    try
                    {
                        SpiderNewsDao.UpdateSpiderNewsListInfo(connection, newsItem);
                    }
                    catch (Exception exception)
                    {
                        Logger.LogError("[DfNewsContentJob] UpdateSpiderNewsListInfo err  : {} ,  url : {}", exception,
                            newsItem.NewsUrl);
                    }
            }
            finally
            {
                connection.Ado.RollbackTran();
            }
    }


    // 直接通过newslist db 拉去数据， 不落 origin db 中
    public void SyncDfContentInfoFromNewsList(int pageSize, List<int> newsStatus)
    {
        using var connection = Pg.Connection();
        var newsListInfos = connection.Queryable<SpiderNewsListModel>()
            .Where(it => newsStatus.Contains(it.DownloadStatusCode) && it.FromMedia == (int)FromTypeOfNews.DfMedia)
            .OrderBy(item => item.NewsTime, OrderByType.Desc).Take(pageSize).ToList();
        var spiderContent = new DfContentSpider();
        foreach (var newsListItem in newsListInfos)
            try
            {
                if (newsListItem.NewsUrl == "") continue;
                var dfContentInfo = spiderContent.GetDfContextInfoByUrlInterface(newsListItem.NewsUrl ?? "")
                    .Result;
                var imgDbList = dfContentInfo.ImgInfos.Select(item => new SpiderNewsImageListModel
                        { NewsUrl = item.NewsUrl, ImageResourceUrl = item.ResourceUrl, ImageName = item.ImgName })
                    .ToList();
                newsListItem.DownloadStatusCode = (int)NewsDownloadStatusCode.SuccessSyncDetailInfo;
                connection.Ado.BeginTran();
                SpiderNewsDao.UpsetSpiderNewsContent(connection, dfContentInfo.ToSpiderNewsContentModel());
                SpiderNewsDao.UpsetSpiderNewsImageList(connection, imgDbList);
                SpiderNewsDao.UpdateSpiderNewListDownloadStatus(connection, newsListItem);
                connection.Ado.CommitTran();
            }
            catch (AggregateException e)
            {
                switch (e.InnerException)
                {
                    case DownloadHttpRequestException:
                        Logger.LogError("[DfNewsContentJob] download  err  : {} , url : {} ", e, newsListItem.NewsUrl);
                        newsListItem.DownloadStatusCode = (int)NewsDownloadStatusCode.FailedSyncDetailInfo;
                        break;
                    case HtmlFormException:
                        Logger.LogError("[DfNewsContentJob] content  html form err : {} ,  url : {}", e,
                            newsListItem.NewsUrl);
                        newsListItem.DownloadStatusCode = (int)NewsDownloadStatusCode.FailedSyncDetailInfo;
                        break;
                    default:
                        Logger.LogError("[DfNewsContentJob] content  unknow other err : {} ,  url : {}", e,
                            newsListItem.NewsUrl);
                        newsListItem.DownloadStatusCode = (int)NewsDownloadStatusCode.FailedSyncDetailInfo;
                        break;
                }

                try
                {
                    SpiderNewsDao.UpdateSpiderNewListDownloadStatus(connection, newsListItem);
                }
                catch (Exception exception)
                {
                    Logger.LogError("[DfNewsContentJob] UpdateSpiderNewsListInfo err  : {}", exception);
                }
            }
            catch (KDbException e)
            {
                Logger.LogError("[DfNewsContentJob] content  db err : {} ,  url : {}", e, newsListItem.NewsUrl);
            }
            catch (Exception e)
            {
                Logger.LogError("[DfNewsContentJob] system err : {} , listInfo : {}", e, newsListItem);
            }
            finally
            {
                connection.Ado.RollbackTran();
            }
    }

    public override Task Execute(IJobExecutionContext context)
    {
        return Task.Run(() =>
        {
            SyncDfContentInfoFromOriginInfo(1000, [(int)NewsDownloadStatusCode.SuccessDownloadOriginInfo]);
        });
        // return Task.Run(() => { SyncDfContentInfoFromNewsList(1000, [(int)NewsDownloadStatusCode.NoDownload]); });
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
        return JobBuilder.Create<DfNewsContentJob>().WithIdentity(JobName + ".Job", jobGroup + ".Job")
            .DisallowConcurrentExecution() // 禁止并发执行
            .WithDescription(JobDescription).Build();
    }
}