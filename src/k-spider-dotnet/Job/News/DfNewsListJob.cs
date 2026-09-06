using KSpider.Data;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.DfNews;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace KSpider.Job.News;

public class DfNewsListJob(SpiderNewsBatchDao spiderNewsBatchDao, Pg pg, ILogger<DfNewsListJob> logger) : SpiderJob
{
    private const int MaxPageNumber = 4;
    private const int PageSize = 200;

    private readonly DfListSpider _dfListSpider = new();

    public override async Task Execute(IJobExecutionContext context)
    {
        const DfListOrderType orderType = DfListOrderType.ByTime;
        var insertNumber = 0;
        using var connection = pg.Connection();
        foreach (var resourceItem in DfNewsResource.DfListUrlResourceList)
            try
            {
                for (var pageNumber = 1; pageNumber <= MaxPageNumber; pageNumber++)
                {
                    var dfListInfos = await _dfListSpider.GetDfListInfoByPageUrl(resourceItem, pageNumber, PageSize,
                        orderType);
                    if (dfListInfos.Count == 0) break;

                    var dbDfListInfos = dfListInfos.Select(item => item.ToSpiderNewListModel()).ToList();
                    var existsUrls = GetExistsUrls(connection, dbDfListInfos);
                    // 批量 ON CONFLICT DO NOTHING 写入 , 避免逐条查询分流的写放大
                    insertNumber += spiderNewsBatchDao.UpsertSpiderNewsListOnConflict(connection, dbDfListInfos, 200);

                    // 当前页已无新 URL 说明该栏目翻到了存量区间 , 不再继续翻页
                    if (dbDfListInfos.All(item => existsUrls.Contains(item.NewsUrl))) break;
                }
            }
            catch (Exception e)
            {
                logger.LogError("[DfNewsListJob Execute]  run error : {}", e);
            }
        logger.LogInformation("[DfNewsListJob Execute] run success , new insert news {}", insertNumber);
    }

    private static List<string?> GetExistsUrls(SqlSugarClient connection, List<SpiderNewsListModel> newsList)
    {
        var urls = newsList.Select(item => item.NewsUrl).ToList();
        return connection.Queryable<SpiderNewsListModel>()
            .Where(it => urls.Contains(it.NewsUrl))
            .Select(it => it.NewsUrl).ToList();
    }
}
