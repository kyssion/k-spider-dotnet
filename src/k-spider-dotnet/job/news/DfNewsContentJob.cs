using k_spider_dotnet_lib.job;
using k_spider_dotnet_lib.logger;
using k_spider_dotnet.data;
using k_spider_dotnet.data;
using k_spider_dotnet.exception;
using k_spider_dotnet.model;
using k_spider_dotnet.spider;
using k_spider_dotnet.spider.df_news;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace k_spider_dotnet.job.news;

public class DfNewsContentJob : SpiderJob
{
    private const string JobName = "DfNewsContentJob";
    private const string JobDescription = "东方财富网站抓取新闻详情任务";

    private static readonly ILogger Logger = LogFactory.GetLogger<DfNewsContentJob>();

    // 直接通过newslist db 拉去数据， 从 origin db 解析结构化内容
    public void SyncDfContentInfoFromOriginInfo(int pageSize)
    {
        using var connection = Pg.Connection();
        // 待解析 = 原始已下载 (3) + 解析失败但未达重试上限 (2)
        var newsListInfos = connection.Queryable<SpiderNewsListModel>()
            .Where(it => (it.DownloadStatusCode == (int)NewsDownloadStatusCode.SuccessDownloadOriginInfo ||
                          (it.DownloadStatusCode == (int)NewsDownloadStatusCode.FailedSyncDetailInfo &&
                           it.FailCount < NewsPipelineConst.MaxFailCount)) &&
                         it.FromMedia == (int)FromTypeOfNews.DfMedia)
            .OrderBy(item => item.Id, OrderByType.Asc).Take(pageSize).ToList();

        // 一次性批量加载原始内容 , 避免循环内逐条查询
        var newsUrls = newsListInfos.Select(it => it.NewsUrl).ToList();
        var originMap = connection.Queryable<SpiderNewsContentOriginModel>()
            .Where(it => newsUrls.Contains(it.NewsUrl)).ToList()
            .GroupBy(it => it.NewsUrl).ToDictionary(group => group.Key!, group => group.First());

        var spiderContent = new DfContentSpider();
        foreach (var newsItem in newsListInfos)
        {
            if (string.IsNullOrEmpty(newsItem.NewsUrl)) continue;

            // 原始内容缺失 : 退回下载失败状态 , 由 OriginJob 重拉
            if (!originMap.TryGetValue(newsItem.NewsUrl, out var spiderOriginInfo))
            {
                newsItem.DownloadStatusCode = (int)NewsDownloadStatusCode.FailedDownloadOriginInfo;
                newsItem.FailCount += 1;
                UpdateStatus(connection, newsItem);
                Logger.LogWarning("[DfNewsContentJob] origin info not find , back to download , url : {}",
                    newsItem.NewsUrl);
                continue;
            }

            try
            {
                var dfContentInfo = spiderContent.GetContentInfoByJson(spiderOriginInfo.NewsOriginContent ?? "",
                    spiderOriginInfo.NewsUrl ?? "");
                // 列表接口返回的摘要比详情接口的标题占位更完整 , 优先回填
                if (!string.IsNullOrEmpty(newsItem.NewsSummary))
                    dfContentInfo.NewsSummary = newsItem.NewsSummary;
                var imgDbList = dfContentInfo.ImgInfos.Select(item => new SpiderNewsImageListModel
                        { NewsUrl = item.NewsUrl, ImageResourceUrl = item.ResourceUrl, ImageName = item.ImgName })
                    .ToList();
                newsItem.DownloadStatusCode = (int)NewsDownloadStatusCode.SuccessSyncDetailInfo;
                newsItem.FailCount = 0;
                connection.Ado.BeginTran();
                SpiderNewsDao.UpsetSpiderNewsContent(connection, dfContentInfo.ToSpiderNewsContentModel());
                SpiderNewsDao.UpsetSpiderNewsImageList(connection, imgDbList);
                SpiderNewsDao.UpdateSpiderNewsListInfo(connection, newsItem);
                connection.Ado.CommitTran();
            }
            catch (Exception e)
            {
                connection.Ado.RollbackTran();
                switch (e)
                {
                    case KDbException:
                        // 数据库异常不消耗重试次数 , 保持状态等待下一轮
                        Logger.LogError("[DfNewsContentJob] content  db err : {} ,  url : {}", e, newsItem.NewsUrl);
                        break;
                    default:
                        Logger.LogError("[DfNewsContentJob] content  parse err : {} ,  url : {}", e,
                            newsItem.NewsUrl);
                        newsItem.DownloadStatusCode = (int)NewsDownloadStatusCode.FailedSyncDetailInfo;
                        newsItem.FailCount += 1;
                        UpdateStatus(connection, newsItem);
                        break;
                }
            }
        }
    }

    private static void UpdateStatus(SqlSugarClient connection, SpiderNewsListModel newsItem)
    {
        try
        {
            SpiderNewsDao.UpdateSpiderNewListDownloadStatus(connection, newsItem);
        }
        catch (Exception exception)
        {
            Logger.LogError("[DfNewsContentJob] UpdateSpiderNewsListInfo err  : {} ,  url : {}", exception,
                newsItem.NewsUrl);
        }
    }

    public override Task Execute(IJobExecutionContext context)
    {
        return Task.Run(() =>
        {
            SyncDfContentInfoFromOriginInfo(1000);
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
        return JobBuilder.Create<DfNewsContentJob>().WithIdentity(JobName + ".Job", jobGroup + ".Job")
            .DisallowConcurrentExecution() // 禁止并发执行
            .WithDescription(JobDescription).Build();
    }
}
