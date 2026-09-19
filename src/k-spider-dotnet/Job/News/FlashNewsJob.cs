using KSpider.Data;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.FlashNews;
using KSpider.Spider.News;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace KSpider.Job.News;

/// <summary>
///     实时快讯任务 : 各快讯源并行拉取 , 一次拉取即完整数据 , 直接写 spider_flash_news。
///     与网页抓取型管线不同 —— 没有下载/解析阶段、没有状态机 :
///     拉到即终态 , 失败记日志后由下一轮 ( 15 秒后 ) 自然重试。
/// </summary>
public class FlashNewsJob(SpiderNewsBatchDao spiderNewsBatchDao, Pg pg, ILogger<FlashNewsJob> logger) : SpiderJob
{
    /// <summary>
    ///     单栏目每轮最多翻页数 : 平时第一页全是已存在 URL 直接停 ;
    ///     停机回补时按此页数向更早翻 ( 一页 20~50 条 , 四页约覆盖数小时 )
    /// </summary>
    private const int MaxPageNumber = 4;

    private const int PageSize = 50;

    public override async Task Execute(IJobExecutionContext context)
    {
        // 各源并行 , 每源独立连接 ( SqlSugarClient 非线程安全 ) , 单源失败不影响其它源
        var results = await Task.WhenAll(FlashNewsSpiderRegistry.All.Select(RunSourceAsync));
        logger.LogInformation("[FlashNewsJob Execute] run success , upsert flash news {} , source detail : {}",
            results.Sum(item => item.InsertNumber),
            string.Join(" | ", results.Select(item => $"{item.FromMedia}={item.InsertNumber}")));
    }

    /// <summary>
    ///     拉取单个快讯源的全部栏目 , 返回本轮写入行数 ; 不向外抛异常 ( 同 NewsListJob 的隔离约定 )
    /// </summary>
    private async Task<(FromTypeOfNews FromMedia, int InsertNumber)> RunSourceAsync(IFlashNewsSpider spider)
    {
        var insertNumber = 0;
        try
        {
            using var connection = pg.Connection();
            foreach (var column in spider.Columns)
                try
                {
                    insertNumber += await RunColumnAsync(connection, spider, column);
                }
                catch (Exception e)
                {
                    logger.LogError("[FlashNewsJob Execute] run error , source : {} , column : {} , err : {}",
                        spider.FromMedia, column.ColumnId, e);
                }
        }
        catch (Exception e)
        {
            logger.LogError("[FlashNewsJob Execute] source failed , source : {} , err : {}", spider.FromMedia, e);
        }

        return (spider.FromMedia, insertNumber);
    }

    /// <summary>
    ///     拉取单个栏目 : 按游标翻页 , 每页一条批量 upsert。
    ///     DO UPDATE 让首页重复拉回的条目带上源侧的修正 / 补充 ( 快讯常在发布后数分钟内更新 )。
    /// </summary>
    private async Task<int> RunColumnAsync(SqlSugarClient connection, IFlashNewsSpider spider, NewsColumn column)
    {
        var insertNumber = 0;
        string? cursor = null;
        for (var pageNumber = 1; pageNumber <= MaxPageNumber; pageNumber++)
        {
            var page = await spider.GetFlashPage(column, PageSize, cursor);
            if (page.Items.Count == 0) break;

            var existsKeys = GetExistsKeys(connection, page.Items);
            insertNumber += spiderNewsBatchDao.UpsertFlashNewsOnConflict(connection, page.Items, 200);

            // 当前页已全部存在说明翻到了存量区间 , 不再继续翻页
            if (page.Items.All(item => existsKeys.Contains($"{item.FromMedia}|{item.NewsUrl}"))) break;
            cursor = page.NextCursor;
            if (cursor == null) break;
        }

        return insertNumber;
    }

    private static HashSet<string> GetExistsKeys(SqlSugarClient connection, List<SpiderFlashNewsModel> flashNews)
    {
        // 各源 URL 互不重叠 , 只按本源的 URL 查存在性即可
        var fromMedia = flashNews[0].FromMedia;
        var urls = flashNews.Select(item => item.NewsUrl).ToList();
        return connection.Queryable<SpiderFlashNewsModel>()
            .Where(it => it.FromMedia == fromMedia && urls.Contains(it.NewsUrl))
            .Select(it => it.NewsUrl).ToList()
            .Select(url => $"{fromMedia}|{url}")
            .ToHashSet();
    }
}
