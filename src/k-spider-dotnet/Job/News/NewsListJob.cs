using KSpider.Data;
using KSpider.Model;
using KSpider.Spider.News;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace KSpider.Job.News;

/// <summary>
///     新闻列表任务 : 遍历全部在管源的栏目抓列表 , 批量写入 spider_news_list ( status=0 )
/// </summary>
public class NewsListJob(SpiderNewsBatchDao spiderNewsBatchDao, Pg pg, ILogger<NewsListJob> logger) : SpiderJob
{
    private const int MaxPageNumber = 4;
    private const int PageSize = 200;

    public override async Task Execute(IJobExecutionContext context)
    {
        var insertNumber = 0;
        using var connection = pg.Connection();
        foreach (var spider in NewsSpiderRegistry.All)
        foreach (var column in spider.Columns)
            try
            {
                for (var pageNumber = 1; pageNumber <= MaxPageNumber; pageNumber++)
                {
                    var newsList = await spider.GetListPage(column, pageNumber, PageSize);
                    if (newsList.Count == 0) break;

                    var existsUrls = GetExistsUrls(connection, newsList);
                    // 批量 ON CONFLICT DO NOTHING 写入 , 避免逐条查询分流的写放大
                    insertNumber += spiderNewsBatchDao.UpsertSpiderNewsListOnConflict(connection, newsList, 200);

                    // 当前页已无新 URL 说明该栏目翻到了存量区间 , 不再继续翻页
                    if (newsList.All(item => existsUrls.Contains(item.NewsUrl))) break;
                }
            }
            catch (Exception e)
            {
                logger.LogError("[NewsListJob Execute] run error , source : {} , column : {} , err : {}",
                    spider.FromMedia, column.ColumnId, e);
            }

        logger.LogInformation("[NewsListJob Execute] run success , new insert news {}", insertNumber);
    }

    private static List<string?> GetExistsUrls(SqlSugarClient connection, List<SpiderNewsListModel> newsList)
    {
        var urls = newsList.Select(item => item.NewsUrl).ToList();
        return connection.Queryable<SpiderNewsListModel>()
            .Where(it => urls.Contains(it.NewsUrl))
            .Select(it => it.NewsUrl).ToList();
    }
}
