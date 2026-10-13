using KSpider.Data;
using KSpider.Model;
using KSpider.Spider.Dataset.LootDrop;
using Microsoft.Extensions.Logging;
using Quartz;

namespace KSpider.Job.Dataset;

/// <summary>
///     数据集任务 ( 第七管线 ) : loot-drop.io 案例库全量拉取直写 spider_lootdrop_startup。
///     与新闻类管线的差异 : 有界集合 + 行会修订 —— 每轮全量即对账 ,
///     写入由 content_hash 变更检测吸收 ( 只有内容真变的行被 UPDATE )。
///     拉取/写库失败直接上抛 , 由 JobRuntimeListener 记失败 ( 下一轮 24 小时后自然重试 )。
/// </summary>
public class LootDropDatasetJob(SpiderLootDropStartupDao datasetDao, Pg pg, ILogger<LootDropDatasetJob> logger)
    : SpiderJob
{
    /// <summary>分批写库行数 : 与研报同款 , 控制单条 SQL 体积 ( 约 8KB/行 × 100 行 ≈ 1MB/批 )</summary>
    private const int MaxBatchNumber = 100;

    private readonly LootDropDatasetSpider _spider = new();

    public override async Task Execute(IJobExecutionContext context)
    {
        var startups = await _spider.GetAllStartupsAsync();
        int changed;
        using (var connection = pg.Connection())
        {
            changed = datasetDao.UpsertLootDropStartups(connection, startups, MaxBatchNumber);
        }

        RunSummary = $"全量 {startups.Count} 行 , 写入(新增+变更) {changed}";
        logger.LogInformation("[LootDropDatasetJob Execute] run success , total {} , changed {}",
            startups.Count, changed);
    }
}
