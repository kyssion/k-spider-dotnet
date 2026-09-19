using KSpider.Data;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.News.Web;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace KSpider.Job.News.Web;

/// <summary>
///     网页抓取型新闻的列表任务 : 各 media 并行抓取栏目列表 , 批量写入 spider_news_list ( status=0 ) ,
///     下载与解析由后续两个任务接力。实时快讯走 FlashNewsJob , 不在本任务范围。
/// </summary>
public class NewsListJob(SpiderNewsBatchDao spiderNewsBatchDao, Pg pg, ILogger<NewsListJob> logger) : SpiderJob
{
    private const int MaxPageNumber = 4;
    private const int PageSize = 200;

    public override async Task Execute(IJobExecutionContext context)
    {
        // media 之间并行 : 每个源有自己的连接、事务与推进节奏 , 互不等待
        // ( 任务带 DisallowConcurrentExecution , 本轮不会与上一轮重叠 )
        var results = await Task.WhenAll(NewsSpiderRegistry.All.Select(RunSourceAsync));
        logger.LogInformation("[NewsListJob Execute] run success , new insert news {} , source detail : {}",
            results.Sum(item => item.InsertNumber),
            string.Join(" | ", results.Select(item => $"{item.FromMedia}={item.InsertNumber}")));
    }

    /// <summary>
    ///     抓取单个 media : 逐栏目按游标翻页 ( 与并发改造前的单源逻辑完全一致 ) , 返回本轮写入行数。
    ///     不向外抛异常 : 单个 media 失败不应影响其它 media , 也不应吞掉整轮汇总日志。
    /// </summary>
    private async Task<(FromTypeOfNews FromMedia, int InsertNumber)> RunSourceAsync(INewsSpider spider)
    {
        var insertNumber = 0;
        try
        {
            // 每个 media 独占一个连接 : SqlSugarClient 不是线程安全的 , 且各 media 的事务互不牵连
            using var connection = pg.Connection();
            foreach (var column in spider.Columns)
                try
                {
                    insertNumber += await RunColumnAsync(connection, spider, column);
                }
                catch (Exception e)
                {
                    logger.LogError("[NewsListJob Execute] run error , source : {} , column : {} , err : {}",
                        spider.FromMedia, column.ColumnId, e);
                }
        }
        catch (Exception e)
        {
            logger.LogError("[NewsListJob Execute] source failed , source : {} , err : {}", spider.FromMedia, e);
        }

        return (spider.FromMedia, insertNumber);
    }

    /// <summary>
    ///     抓取单个栏目 : 按游标翻页 , 每页一条批量 upsert ( 单条语句自带原子性 , 不需要显式事务 )
    /// </summary>
    private async Task<int> RunColumnAsync(SqlSugarClient connection, INewsSpider spider, NewsColumn column)
    {
        var insertNumber = 0;
        string? cursor = null;
        for (var pageNumber = 1; pageNumber <= MaxPageNumber; pageNumber++)
        {
            var listPage = await spider.GetListPage(column, PageSize, cursor);
            if (listPage.Items.Count == 0) break;

            var existsUrls = GetExistsUrls(connection, listPage.Items);
            // 批量 ON CONFLICT DO NOTHING 写入 , 避免逐条查询分流的写放大
            insertNumber += spiderNewsBatchDao.UpsertSpiderNewsListOnConflict(connection, listPage.Items, 200);

            // 当前页已无新 URL 说明该栏目翻到了存量区间 , 不再继续翻页
            if (listPage.Items.All(item => existsUrls.Contains(item.NewsUrl))) break;
            cursor = listPage.NextCursor;
            if (cursor == null) break;
        }

        return insertNumber;
    }

    private static List<string?> GetExistsUrls(SqlSugarClient connection, List<SpiderNewsListModel> newsList)
    {
        var urls = newsList.Select(item => item.NewsUrl).ToList();
        return connection.Queryable<SpiderNewsListModel>()
            .Where(it => urls.Contains(it.NewsUrl))
            .Select(it => it.NewsUrl).ToList();
    }
}
