using KSpider.Data;
using KSpider.Exceptions;
using KSpider.Spider;
using KSpider.Spider.Policy;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace KSpider.Job.Policy;

/// <summary>
///     官方文件任务 : 政府/官方机构发布的文件与政策 ( 统计局/证监会… 每站一 Spider ) ,
///     列表存根 → 存在性判定 → 逐篇下载详情组装完整行 , 直写 spider_policy_document
///     ( ON CONFLICT DO NOTHING , 发布即终态 )。
///     翻页停止判定 : 当前页 doc_id 全部已存在即停 ( 列表从新到旧 , 首页全旧 = 后面更旧 ;
///     统计局/证监会当前均为单页全量列表 )。
/// </summary>
public class PolicyJob(SpiderPolicyDao policyDao, Pg pg, ILogger<PolicyJob> logger) : SpiderJob
{
    /// <summary>每源每栏目每轮最多翻页数 : 两源当前均为单页全量 , 此上限防未来分页站点失控</summary>
    private const int MaxPageNumber = 10;

    public override async Task Execute(IJobExecutionContext context)
    {
        var inserted = 0;
        var failedSources = 0;
        using var connection = pg.Connection();
        foreach (var spider in PolicySpiderRegistry.All)
            try
            {
                inserted += await RunSpiderAsync(connection, spider);
            }
            catch (Exception e)
            {
                failedSources++;
                logger.LogError("[PolicyJob Execute] source : {} , err : {}", spider.FromMedia, e);
            }

        RunSummary = $"官方文件新增 {inserted}";
        logger.LogInformation("[PolicyJob Execute] run success , insert {} , failedSources {}", inserted, failedSources);
    }

    /// <summary>
    ///     拉取单源全部栏目 : 每栏目翻页拉存根 → 当前页全旧即停 → 逐篇下载详情组装完整行 upsert
    /// </summary>
    private async Task<int> RunSpiderAsync(SqlSugarClient connection, IPolicySpider spider)
    {
        var inserted = 0;
        foreach (var column in spider.Columns)
            try
            {
                inserted += await RunColumnAsync(connection, spider, column);
            }
            catch (Exception e)
            {
                logger.LogError("[PolicyJob RunSpiderAsync] source : {} , column : {} , err : {}",
                    spider.FromMedia, column.ColumnId, e);
            }

        return inserted;
    }

    private async Task<int> RunColumnAsync(SqlSugarClient connection, IPolicySpider spider, SourceColumn column)
    {
        var inserted = 0;
        for (var pageNo = 1; pageNo <= MaxPageNumber; pageNo++)
        {
            var page = await spider.GetDocumentPage(column, pageNo);
            if (page.Items.Count == 0) break;

            // 存在性判定在详情下载之前 : 当前页全旧即停 , 避免逐篇空跑
            var existsIds = policyDao.GetExistsDocIds(connection, (int)spider.FromMedia,
                page.Items.Select(item => item.DocId).ToList());
            var newStubs = page.Items.Where(item => !existsIds.Contains(item.DocId)).ToList();

            foreach (var stub in newStubs)
                try
                {
                    var document = await spider.GetDocumentAsync(stub);
                    inserted += policyDao.UpsertPolicyDocumentOnConflict(connection, [document], 1);
                }
                catch (Exception e)
                {
                    // 单篇详情失败不中断整列 ( 政府站点可用性波动常见 , 下一轮自然重试 )
                    logger.LogError("[PolicyJob RunColumnAsync] 详情失败 , source : {} , docId : {} , err : {}",
                        spider.FromMedia, stub.DocId, e);
                }

            if (newStubs.Count < page.Items.Count) break; // 页内已有存在行 , 后面更旧
            if (!page.HasMore) break;
        }

        return inserted;
    }
}
