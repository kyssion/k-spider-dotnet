using KSpider.Data;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.News;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace KSpider.Job.News;

/// <summary>
///     新闻列表任务 : 遍历全部在管源的栏目抓列表 , 批量写入 spider_news_list ( status=0 )
///     列表接口已带全文的源 ( 快讯型 ) 同时落原始内容并直接置为已下载 ( status=3 )
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
                string? cursor = null;
                for (var pageNumber = 1; pageNumber <= MaxPageNumber; pageNumber++)
                {
                    var listPage = await spider.GetListPage(column, PageSize, cursor);
                    if (listPage.Items.Count == 0) break;

                    var existsUrls = GetExistsUrls(connection, listPage.Items);
                    MarkInlineOriginItems(listPage);
                    connection.Ado.BeginTran();
                    // 批量 ON CONFLICT DO NOTHING 写入 , 避免逐条查询分流的写放大
                    insertNumber += spiderNewsBatchDao.UpsertSpiderNewsListOnConflict(connection, listPage.Items, 200);
                    // 快讯型源的原始内容必须与列表行同一事务 , 否则会留下"已置为已下载却没有 origin"的悬空行
                    if (listPage.InlineOrigins.Count > 0)
                        spiderNewsBatchDao.UpsetSpiderNewsContentOriginOnConflict(connection,
                            listPage.InlineOrigins.Select(item => item.ToModel()).ToList(), 200);
                    connection.Ado.CommitTran();

                    // 当前页已无新 URL 说明该栏目翻到了存量区间 , 不再继续翻页
                    if (listPage.Items.All(item => existsUrls.Contains(item.NewsUrl))) break;
                    cursor = listPage.NextCursor;
                    if (cursor == null) break;
                }
            }
            catch (Exception e)
            {
                // 无活动事务时 RollbackTran 是安全空操作
                connection.Ado.RollbackTran();
                logger.LogError("[NewsListJob Execute] run error , source : {} , column : {} , err : {}",
                    spider.FromMedia, column.ColumnId, e);
            }

        logger.LogInformation("[NewsListJob Execute] run success , new insert news {}", insertNumber);
    }

    /// <summary>
    ///     快讯型源的列表项已带全文 : 置为已下载 , 让这些行跳过下载阶段直接进解析阶段
    /// </summary>
    private static void MarkInlineOriginItems(NewsListPage listPage)
    {
        if (listPage.InlineOrigins.Count == 0) return;
        var inlineUrls = listPage.InlineOrigins.Select(item => item.NewsUrl).ToHashSet();
        foreach (var item in listPage.Items.Where(item => item.NewsUrl != null && inlineUrls.Contains(item.NewsUrl)))
            item.DownloadStatusCode = (int)NewsDownloadStatusCode.SuccessDownloadOriginInfo;
    }

    private static List<string?> GetExistsUrls(SqlSugarClient connection, List<SpiderNewsListModel> newsList)
    {
        var urls = newsList.Select(item => item.NewsUrl).ToList();
        return connection.Queryable<SpiderNewsListModel>()
            .Where(it => urls.Contains(it.NewsUrl))
            .Select(it => it.NewsUrl).ToList();
    }
}
