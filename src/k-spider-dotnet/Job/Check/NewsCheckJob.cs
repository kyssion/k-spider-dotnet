using System.Text.Json;
using KSpider.Data;
using KSpider.Job.Node;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.News.Flash;
using KSpider.Spider.News.Web;
using KSpider.Spider.Verify;
using KSpider.Spider.Verify.Model;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace KSpider.Job.Check;

/// <summary>
///     爬虫健康检查任务 :
///     1. 各源栏目接口可用性探测 ( 网页型 + 快讯型 )
///     2. 网页型流水线积压 / 失败统计
///     3. 快讯源实时性滞后监控 ( 最新一条距现在多久 )
///     4. 反爬验证阻塞告警 ( 识别到验证但自动通过失败 )
///     5. 节点状态快照上报 : 以上统计汇总 upsert 到 spider_node_status , 供 Web 控制台展示
/// </summary>
public class NewsCheckJob(Pg pg, SystemStatusDao systemStatusDao, ILogger<NewsCheckJob> logger) : SpiderJob
{
    /// <summary>快照 payload 的序列化风格 ( camelCase , 与 Web 端解析约定一致 )</summary>
    private static readonly JsonSerializerOptions PayloadJsonOptions = new(JsonSerializerDefaults.Web);

    public override Task Execute(IJobExecutionContext context)
    {
        return Task.Run(async () =>
        {
            var apiErrors = await CheckListApiAlive();
            var backlog = CheckPipelineBacklog();
            var flashLag = CheckFlashNewsLag();
            var verifyBlocked = CheckVerificationBlocked();
            ReportNodeStatus(apiErrors, backlog, flashLag, verifyBlocked);
        });
    }

    /// <summary>
    ///     逐个源的栏目探测接口是否仍返回有效数据 ( 防止源改版后静默失效 ) ; 返回失败清单
    /// </summary>
    private async Task<List<string>> CheckListApiAlive()
    {
        var errors = new List<string>();
        foreach (var spider in NewsSpiderRegistry.All)
        foreach (var column in spider.Columns)
            try
            {
                var listPage = await spider.GetListPage(column, 10, null);
                if (listPage.Items.Count == 0) throw new Exception("接口未返回数据");
            }
            catch (Exception e)
            {
                errors.Add($"[{(int)spider.FromMedia} {spider.FromMedia}] column {column.ColumnId} : {e.Message}");
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
                errors.Add($"[{(int)spider.FromMedia} {spider.FromMedia}] column {column.ColumnId} : {e.Message}");
                logger.LogError("[NewsCheckJob CheckListApiAlive] api err : {} , source : {} , column : {}", e,
                    spider.FromMedia, column.ColumnId);
            }

        return errors;
    }

    /// <summary>
    ///     网页型流水线 : 按源统计各状态的数量与最老待处理新闻的滞留时长
    /// </summary>
    private List<PipelineBacklogItem> CheckPipelineBacklog()
    {
        var result = new List<PipelineBacklogItem>();
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
                result.Add(new PipelineBacklogItem(fromMedia,
                    statusCounts.ToDictionary(it => it.DownloadStatusCode.ToString(), it => it.Count)));
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

        return result;
    }

    /// <summary>
    ///     快讯源实时性 : 最新一条距现在多久。快讯的价值在实时 ,
    ///     滞后超过两轮轮询 ( 30 秒 ) 说明该源接口异常或长时间无发布 , 需要关注。
    /// </summary>
    private List<FlashLagItem> CheckFlashNewsLag()
    {
        var result = new List<FlashLagItem>();
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
                result.Add(new FlashLagItem(fromMedia, newest.ToString("yyyy-MM-dd HH:mm:ss"), lagSeconds));
            }
        }
        catch (Exception e)
        {
            logger.LogError("[NewsCheckJob CheckFlashNewsLag] err : {}", e);
        }

        return result;
    }

    /// <summary>
    ///     反爬验证阻塞 : 处于验证冷却期的源会持续拿不到数据 , 必须让人看见 ——
    ///     被反爬拦住最糟的结果不是失败 , 而是悄悄返回空数据、看起来一切正常。
    /// </summary>
    private IReadOnlyList<VerificationBlock> CheckVerificationBlocked()
    {
        var blockedHosts = VerificationRegistry.Pipeline.BlockedHosts();
        if (blockedHosts.Count == 0)
        {
            logger.LogInformation("[NewsCheckJob CheckVerificationBlocked] no host blocked by verification");
            return [];
        }

        foreach (var block in blockedHosts)
            logger.LogWarning(
                "[NewsCheckJob CheckVerificationBlocked] host : {Host} , kind : {Kind} , until : {Until:yyyy-MM-dd HH:mm:ss} , reason : {Reason}",
                block.Host, block.Kind, block.Until, block.Reason);
        return blockedHosts;
    }

    /// <summary>
    ///     汇总本轮全部统计 upsert 节点快照 ; 失败只记日志 ( 下一轮 5 分钟后自然重试 )
    /// </summary>
    private void ReportNodeStatus(List<string> apiErrors, List<PipelineBacklogItem> backlog,
        List<FlashLagItem> flashLag, IReadOnlyList<VerificationBlock> verifyBlocked)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                apiAliveErrors = apiErrors,
                pipelineBacklog = backlog,
                flashLag,
                verifyBlocked = verifyBlocked.Select(block => new
                {
                    block.Host,
                    Kind = block.Kind.ToString(),
                    Until = block.Until.ToString("yyyy-MM-dd HH:mm:ss"),
                    block.Reason
                })
            }, PayloadJsonOptions);
            using var connection = pg.Connection();
            systemStatusDao.UpsertNodeStatus(connection, NodeIdentity.NodeId, payload);
            RunSummary = $"接口异常 {apiErrors.Count} , 验证冷却 {verifyBlocked.Count}";
        }
        catch (Exception e)
        {
            logger.LogError("[NewsCheckJob ReportNodeStatus] err : {}", e);
        }
    }

    /// <summary>管线积压统计 ( 单源 ) : source 为 from_media 编号 , statusCounts 的键为状态码</summary>
    private sealed record PipelineBacklogItem(int Source, Dictionary<string, int> StatusCounts);

    /// <summary>快讯滞后统计 ( 单源 )</summary>
    private sealed record FlashLagItem(int Source, string Newest, int LagSeconds);
}
