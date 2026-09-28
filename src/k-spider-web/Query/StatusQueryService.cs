using KSpider.Data;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.News.Flash;
using SqlSugar;

namespace KSpider.Web.Query;

/// <summary>
///     运行状态查询 ( 只读 ) : 任务调度态 / 管线积压 / 快讯滞后 / 节点快照 , 全部来自爬虫进程上报的系统表
/// </summary>
public class StatusQueryService(Pg pg)
{
    /// <summary>全部任务的调度态 ( 执行结果列 + 调度列 ) , 按节点、任务名排序</summary>
    public List<SpiderJobStateModel> JobStates()
    {
        using var connection = pg.Connection();
        return connection.Queryable<SpiderJobStateModel>()
            .OrderBy(it => it.NodeId).OrderBy(it => it.JobName).ToList();
    }

    /// <summary>节点快照列表 : payload 为 JSON 原文 ( 接口探测 / 积压 / 滞后 / 验证冷却 ) , 前端解析</summary>
    public List<SpiderNodeStatusModel> Nodes()
    {
        using var connection = pg.Connection();
        return connection.Queryable<SpiderNodeStatusModel>().OrderBy(it => it.NodeId).ToList();
    }

    /// <summary>
    ///     网页管线积压 : 按源统计各状态数量与最老待处理时间 ( 口径同 NewsCheckJob )
    /// </summary>
    public List<PipelineStatusDto> Pipeline()
    {
        using var connection = pg.Connection();
        var counts = connection.Queryable<SpiderNewsListModel>()
            .GroupBy(it => new { it.FromMedia, it.DownloadStatusCode })
            .Select(it => new
            {
                it.FromMedia,
                it.DownloadStatusCode,
                Count = SqlFunc.AggregateCount(it.Id)
            }).ToList();
        var oldestPending = connection.Queryable<SpiderNewsListModel>()
            .Where(it => it.DownloadStatusCode == (int)NewsDownloadStatusCode.NoDownload)
            .GroupBy(it => it.FromMedia)
            .Select(it => new { it.FromMedia, Oldest = SqlFunc.AggregateMin(it.NewsTime) }).ToList();
        return counts.GroupBy(it => it.FromMedia)
            .Select(group => new PipelineStatusDto(
                group.Key ?? 0,
                group.ToDictionary(it => it.DownloadStatusCode, it => it.Count),
                oldestPending.FirstOrDefault(it => it.FromMedia == group.Key)?.Oldest))
            .OrderBy(it => it.Source).ToList();
    }

    /// <summary>快讯源实时性 : 各源最新一条与滞后秒数 ( 源列表来自快讯注册表 )</summary>
    public List<FlashLagDto> FlashLag()
    {
        using var connection = pg.Connection();
        var result = new List<FlashLagDto>();
        foreach (var spider in FlashNewsSpiderRegistry.All)
        {
            var fromMedia = (int)spider.FromMedia;
            var newest = connection.Queryable<SpiderFlashNewsModel>()
                .Where(it => it.FromMedia == fromMedia)
                .Max(it => it.NewsTime);
            // 无数据时不返回 0001-01-01 占位 , 直接给 null
            result.Add(new FlashLagDto(fromMedia, spider.FromMedia.ToString(),
                newest == default ? null : newest,
                newest == default ? null : (int)(DateTime.Now - newest).TotalSeconds));
        }

        return result;
    }
}

/// <summary>管线积压统计 ( 单源 ) : statusCounts 键为状态码 ( 0/1/2/3/4 )</summary>
public sealed record PipelineStatusDto(int Source, Dictionary<int, int> StatusCounts, DateTime? OldestPending);

/// <summary>快讯源滞后统计 ( 单源 ) ; Newest 为空表示该源还没有数据</summary>
public sealed record FlashLagDto(int Source, string SourceName, DateTime? Newest, int? LagSeconds);
