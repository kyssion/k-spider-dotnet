using System.Text.Json.Nodes;
using k_spider_dotnet.job;
using k_spider_dotnet.logger;
using k_spider_dotnet.data;
using k_spider_dotnet.model;
using k_spider_dotnet.spider;
using k_spider_dotnet.spider.df_news;
using k_spider_dotnet.tool.http;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace k_spider_dotnet.job.check;

/// <summary>
///     爬虫健康检查任务 : 1. 各栏目列表接口可用性探测 2. 新闻流水线积压/失败统计
/// </summary>
public class DfCheckJob : SpiderJob
{
    private const string JobName = "DfCheckJob";
    private const string JobDescription = "东方财富网站抓取新闻列表check任务";

    private static readonly ILogger Logger = LogFactory.GetLogger<DfCheckJob>();

    public override Task Execute(IJobExecutionContext context)
    {
        return Task.Run(async () =>
        {
            await CheckListApiAlive();
            CheckPipelineBacklog();
        });
    }

    /// <summary>
    ///     探测 33 个栏目列表接口是否仍返回有效数据 ( 防止东财改版后静默失效 )
    /// </summary>
    private static async Task CheckListApiAlive()
    {
        const DfListOrderType orderType = DfListOrderType.ByTime;
        foreach (var resourceItem in DfNewsResource.DfListUrlResourceList)
            try
            {
                var urlNow = string.Format(DfNewsResource.RequestDfListUrl, resourceItem.ListResourceNumber,
                    (int)orderType, 1, 10, DateTime.Now.Millisecond);
                var responseString =
                    await HttpClientTools.CreateByHost(DfNewsResource.ListResourceHost).GetStringAsync(urlNow);
                var jsonDataList = (JsonArray?)JsonNode.Parse(responseString)?["data"]?["list"];
                if (jsonDataList == null || jsonDataList.Count == 0)
                    throw new Exception("not find date");
            }
            catch (Exception e)
            {
                Logger.LogError("[CheckListApiAlive] api err : {} , resourceItem id : {}", e,
                    resourceItem.ListResourceNumber);
            }
    }

    /// <summary>
    ///     统计新闻流水线各状态的数量与最老待处理新闻的滞留时长
    /// </summary>
    private static void CheckPipelineBacklog()
    {
        try
        {
            using var connection = Pg.Connection();
            var statusCounts = connection.Queryable<SpiderNewsListModel>()
                .Where(it => it.FromMedia == (int)FromTypeOfNews.DfMedia)
                .GroupBy(it => it.DownloadStatusCode)
                .Select(it => new
                {
                    it.DownloadStatusCode,
                    Count = SqlFunc.AggregateCount(it.Id)
                }).ToList();
            var oldestPending = connection.Queryable<SpiderNewsListModel>()
                .Where(it => it.DownloadStatusCode == (int)NewsDownloadStatusCode.NoDownload)
                .Min(it => it.NewsTime);
            var statusText = string.Join(" , ",
                statusCounts.Select(it => $"{(NewsDownloadStatusCode)it.DownloadStatusCode}={it.Count}"));
            Logger.LogInformation("[CheckPipelineBacklog] status : {Status} , oldest pending news time : {Oldest}",
                statusText, oldestPending);
        }
        catch (Exception e)
        {
            Logger.LogError("[CheckPipelineBacklog] err : {}", e);
        }
    }

    public override ITrigger GetTrigger(string jobGroup, IJobDetail jobDetail)
    {
        return TriggerBuilder.Create().ForJob(jobDetail)
            .WithIdentity(JobName + ".Trigger", jobGroup + ".Trigger").StartNow()
            .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever().Build())
            .Build();
    }

    public override IJobDetail GetJobDetail(string jobGroup)
    {
        return JobBuilder.Create<DfCheckJob>().WithIdentity(JobName + ".Job", jobGroup + ".Job")
            .DisallowConcurrentExecution() // 禁止并发执行
            .WithDescription(JobDescription).Build();
    }
}
