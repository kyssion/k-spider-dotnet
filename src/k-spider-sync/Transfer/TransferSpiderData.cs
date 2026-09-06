using KSpider.Data;
using KSpider.Logger;
using KSpider.Model;
using KSpider.Sync.Job;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace KSpider.Sync.Transfer;

public static class TransferSpiderData
{
    private const int BatchSize = 2000;
    private static readonly ILogger Logger = LogFactory.GetLogger<TransferSpiderDataJob>();

    // 同步远程数据库脚本到本地 ( 复用主项目的 SqlSugar 实体 )
    public static async Task DoTransfer()
    {
        using var remote = Pg.CreateConnection(TransferConfig.RemoteConnectionString);
        using var local = Pg.CreateConnection(TransferConfig.LocalConnectionString);

        // 单表失败不阻断其余表 , 下一轮任务重试
        await SyncTableSafely<SpiderNewsListModel>(remote, local, "spider_news_list");
        await SyncTableSafely<SpiderNewsContentOriginModel>(remote, local, "spider_news_content_origin");
        await SyncTableSafely<SpiderNewsContentModel>(remote, local, "spider_news_content");
        await SyncTableSafely<SpiderNewsImageListModel>(remote, local, "spider_news_image_list");
        Logger.LogInformation("[DoTransfer] transfer success");
    }

    private static async Task SyncTableSafely<T>(SqlSugarClient remote, SqlSugarClient local, string tableName)
        where T : class, ILongIdEntity, new()
    {
        try
        {
            var insertNumber = await SyncTableById<T>(remote, local);
            if (insertNumber > 0)
                Logger.LogInformation("[DoTransfer] {TableName} sync insert {Number}", tableName, insertNumber);
        }
        catch (Exception e)
        {
            Logger.LogError("[DoTransfer] {TableName} sync err : {Error}", tableName, e);
        }
    }

    /// <summary>
    ///     按自增 Id 增量同步 : 从本地最大 Id 之后分批拉取远端数据 ,
    ///     批内过滤掉本地已存在的 Id 后插入 , 单条坏数据不会中断整表同步
    /// </summary>
    private static async Task<int> SyncTableById<T>(SqlSugarClient remote, SqlSugarClient local)
        where T : class, ILongIdEntity, new()
    {
        var insertNumber = 0;
        var lastId = local.Queryable<T>().Max(it => it.Id);
        while (true)
        {
            var batch = await remote.Queryable<T>()
                .Where(it => it.Id > lastId)
                .OrderBy(it => it.Id)
                .Take(BatchSize).ToListAsync();
            if (batch.Count == 0) break;

            var batchIds = batch.Select(it => it.Id).ToList();
            var localExistsIds = await local.Queryable<T>()
                .Where(it => batchIds.Contains(it.Id))
                .Select(it => it.Id).ToListAsync();
            var insertItems = batch.Where(it => !localExistsIds.Contains(it.Id)).ToList();
            if (insertItems.Count > 0)
            {
                await local.Insertable(insertItems).ExecuteCommandAsync();
                insertNumber += insertItems.Count;
            }

            lastId = batch[^1].Id;
            if (batch.Count < BatchSize) break;
        }

        return insertNumber;
    }
}
