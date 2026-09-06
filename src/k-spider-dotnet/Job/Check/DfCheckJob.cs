using System.Text.Json.Nodes;
using KSpider.Data;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.DfNews;
using KSpider.Tool.Http;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace KSpider.Job.Check;

/// <summary>
///     爬虫健康检查任务 : 1. 各栏目列表接口可用性探测 2. 新闻流水线积压/失败统计
/// </summary>
public class DfCheckJob(Pg pg, ILogger<DfCheckJob> logger) : SpiderJob
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
    ///     探测 33 个栏目列表接口是否仍返回有效数据 ( 防止东财改版后静默失效 )
    /// </summary>
    private async Task CheckListApiAlive()
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
                logger.LogError("[CheckListApiAlive] api err : {} , resourceItem id : {}", e,
                    resourceItem.ListResourceNumber);
            }
    }

    /// <summary>
    ///     统计新闻流水线各状态的数量与最老待处理新闻的滞留时长
    /// </summary>
    private void CheckPipelineBacklog()
    {
        try
        {
            using var connection = pg.Connection();
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
            logger.LogInformation("[CheckPipelineBacklog] status : {Status} , oldest pending news time : {Oldest}",
                statusText, oldestPending);
        }
        catch (Exception e)
        {
            logger.LogError("[CheckPipelineBacklog] err : {}", e);
        }
    }
}
