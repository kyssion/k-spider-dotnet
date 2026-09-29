using System.Text.Json;
using KSpider.Common;
using KSpider.Data;
using KSpider.Model;
using KSpider.Spider.News.Web;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace KSpider.Web.Replay;

/// <summary>
///     重放筛选条件 ( 页面配置 ) : 命中 spider_news_content_origin 的行才会被重新解析。
///     时间窗按 origin 行的 create_time ( 入库时间 ) ; MaxCount 是单次任务的条数上限 , 防手滑全库重放
/// </summary>
public sealed record ReplayFilter(
    int? FromMedia,
    string? ParserCode,
    DateTime? Start,
    DateTime? End,
    int MaxCount = 20000);

/// <summary>
///     数据重放服务 ( Web 进程内异步执行 ) : 按筛选把 origin 行重新解析 ,
///     重写 spider_news_content / spider_news_image_list 并推进列表行状态——网页零重抓 ,
///     解析规则升级 / 数据修复的主路径。任务与进度落 spider_replay_log , 页面轮询展示。
///     同一时刻只允许一个任务 ( 信号量门控 ) , 提交即拒绝第二个。
/// </summary>
public class OriginReplayService(Pg pg, SpiderNewsDao spiderNewsDao, ILogger<OriginReplayService> logger)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private const int BatchSize = 500;

    private static readonly JsonSerializerOptions FilterJsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>预览 : 命中筛选的 origin 行数 ( 不执行重放 )</summary>
    public int Preview(ReplayFilter filter)
    {
        using var connection = pg.Connection();
        return BuildQuery(connection, Normalize(filter)).Count();
    }

    /// <summary>
    ///     提交重放任务 : 写 running 任务行并后台执行 , 返回任务行 ;
    ///     已有任务在跑时抛 InvalidOperationException ( 端点转 409 )
    /// </summary>
    public SpiderReplayLogModel Submit(ReplayFilter filter)
    {
        if (!Gate.Wait(0))
            throw new InvalidOperationException("已有重放任务在运行 , 等它结束后再提交");
        try
        {
            using var connection = pg.Connection();
            var filterJson = JsonSerializer.Serialize(Normalize(filter), FilterJsonOptions);
            var id = connection.Ado.SqlQuery<long>(
                "INSERT INTO spider_replay_log (filter) VALUES (@filter) RETURNING id",
                new { filter = filterJson }).First();
            // 后台执行 , 不占用请求线程 ; 异常与进度都在任务行上可见
            _ = Task.Run(() => Run(id, Normalize(filter)));
            return connection.Queryable<SpiderReplayLogModel>().First(it => it.Id == id);
        }
        catch
        {
            Gate.Release();
            throw;
        }
    }

    /// <summary>最近的重放任务 ( 含进行中的进度 )</summary>
    public List<SpiderReplayLogModel> Recent(int limit)
    {
        using var connection = pg.Connection();
        return connection.Queryable<SpiderReplayLogModel>()
            .OrderByDescending(it => it.Id).Take(Math.Clamp(limit, 1, 50)).ToList();
    }

    /// <summary>
    ///     重放主体 : 按 Id 游标分批 , 逐行路由解析器 → 事务写 content/images → 推进列表行。
    ///     语义与 NewsContentJob 对齐 : 解析成功置列表行 status=1 / fail_count=0 ;
    ///     解析失败**不动列表状态** ( 重放失败通常是解析 bug 或脏数据 , 不该消耗状态机重试次数 ) , 只计数。
    ///     列表行缺失的 origin 仍写 content ( 数据有效 ) , 计入跳过数记到 message。
    /// </summary>
    private void Run(long taskId, ReplayFilter filter)
    {
        var total = 0;
        var success = 0;
        var fail = 0;
        var skipped = 0;
        try
        {
            var lastId = 0L;
            while (total < filter.MaxCount)
            {
                using var connection = pg.Connection();
                var origins = BuildQuery(connection, filter)
                    .Where(it => it.Id > lastId)
                    .OrderBy(it => it.Id)
                    .Take(Math.Min(BatchSize, filter.MaxCount - total))
                    .ToList();
                if (origins.Count == 0) break;
                lastId = origins[^1].Id;

                foreach (var origin in origins)
                {
                    var spider = NewsParserRegistry.Resolve(origin.ParserCode, origin.FromMedia);
                    if (spider == null)
                    {
                        // 码与媒体都路由不到 : 脏数据 , 计失败不动库
                        fail++;
                        total++;
                        continue;
                    }

                    try
                    {
                        var parseResult = spider.ParseContent(origin.NewsOriginContent ?? "", origin.NewsUrl ?? "");
                        connection.Ado.BeginTran();
                        spiderNewsDao.UpsetSpiderNewsContent(connection, parseResult.Content);
                        spiderNewsDao.UpsetSpiderNewsImageList(connection, parseResult.Images);
                        if (AdvanceListRowOnSuccess(connection, origin.NewsUrl ?? ""))
                            skipped++;
                        connection.Ado.CommitTran();
                        success++;
                    }
                    catch (Exception e)
                    {
                        connection.Ado.RollbackTran();
                        fail++;
                        logger.LogError("[OriginReplayService Run] 重放解析失败 , url : {} , err : {}",
                            origin.NewsUrl, e);
                    }

                    total++;
                }

                UpdateProgress(taskId, total, success, fail);
            }

            Finish(taskId, "done",
                skipped == 0 ? null : $"跳过 {skipped} 条 ( 列表行缺失 , 仅写 content )");
        }
        catch (Exception e)
        {
            logger.LogError("[OriginReplayService Run] 重放任务中止 , taskId : {} , err : {}", taskId, e);
            Finish(taskId, "failed", $"中止于 {total} 条 : {e.Message}");
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <summary>
    ///     解析成功后推进列表行 : status=1 / fail_count=0 ;
    ///     返回列表行是否缺失 ( 缺失时只记日志不回滚 , content 已写 , 调用方计入跳过数 )
    /// </summary>
    private bool AdvanceListRowOnSuccess(SqlSugarClient connection, string newsUrl)
    {
        var listRow = connection.Queryable<SpiderNewsListModel>().First(it => it.NewsUrl == newsUrl);
        if (listRow == null)
        {
            logger.LogWarning("[OriginReplayService AdvanceListRowOnSuccess] 列表行缺失 , 仅写 content , url : {}",
                newsUrl);
            return true;
        }

        listRow.DownloadStatusCode = (int)KSpider.Spider.NewsDownloadStatusCode.SuccessSyncDetailInfo;
        listRow.FailCount = 0;
        spiderNewsDao.UpdateSpiderNewListDownloadStatus(connection, listRow);
        return false;
    }

    private ISugarQueryable<SpiderNewsContentOriginModel> BuildQuery(SqlSugarClient connection, ReplayFilter filter)
    {
        var query = connection.Queryable<SpiderNewsContentOriginModel>();
        if (filter.FromMedia != null) query = query.Where(it => it.FromMedia == filter.FromMedia);
        if (!string.IsNullOrEmpty(filter.ParserCode)) query = query.Where(it => it.ParserCode == filter.ParserCode);
        if (filter.Start != null) query = query.Where(it => it.CreateTime >= filter.Start);
        if (filter.End != null) query = query.Where(it => it.CreateTime < filter.End);
        return query;
    }

    /// <summary>筛选规整 : 时间统一 SpecifyKind 为 Unspecified ( 列是 timestamp without time zone ) , 上限钳制</summary>
    private static ReplayFilter Normalize(ReplayFilter filter)
    {
        return filter with
        {
            Start = filter.Start == null ? null : DateTime.SpecifyKind(filter.Start.Value, DateTimeKind.Unspecified),
            End = filter.End == null ? null : DateTime.SpecifyKind(filter.End.Value, DateTimeKind.Unspecified),
            MaxCount = Math.Clamp(filter.MaxCount, 1, 500_000),
            ParserCode = string.IsNullOrWhiteSpace(filter.ParserCode) ? null : filter.ParserCode.Trim(),
            FromMedia = filter.FromMedia
        };
    }

    private void UpdateProgress(long taskId, int total, int success, int fail)
    {
        using var connection = pg.Connection();
        connection.Ado.ExecuteCommand(
            "UPDATE spider_replay_log SET total = @total, success_count = @success, fail_count = @fail WHERE id = @taskId",
            new { total, success, fail, taskId });
    }

    private void Finish(long taskId, string status, string? message)
    {
        using var connection = pg.Connection();
        connection.Ado.ExecuteCommand(
            """
            UPDATE spider_replay_log
            SET status = @status, message = @message, finish_time = CURRENT_TIMESTAMP
            WHERE id = @taskId
            """,
            new { status, message, taskId });
    }
}
