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

        EnsureWatermarkTable(local);

        // 单表失败不阻断其余表 , 下一轮任务重试
        await SyncTableSafely<SpiderNewsListModel>(remote, local, "spider_news_list");
        await SyncTableSafely<SpiderNewsContentOriginModel>(remote, local, "spider_news_content_origin");
        await SyncTableSafely<SpiderNewsContentModel>(remote, local, "spider_news_content");
        await SyncTableSafely<SpiderNewsImageListModel>(remote, local, "spider_news_image_list");
        Logger.LogInformation("[DoTransfer] transfer success");
    }

    private static async Task SyncTableSafely<T>(SqlSugarClient remote, SqlSugarClient local, string tableName)
        where T : class, ILongIdEntity, IUpdateTimeEntity, new()
    {
        try
        {
            var insertNumber = await SyncNewRowsById<T>(remote, local);
            if (insertNumber > 0)
                Logger.LogInformation("[DoTransfer] {TableName} sync insert {Number}", tableName, insertNumber);

            var updateNumber = await SyncUpdatedRowsByUpdateTime<T>(remote, local, tableName);
            if (updateNumber > 0)
                Logger.LogInformation("[DoTransfer] {TableName} sync update {Number}", tableName, updateNumber);
        }
        catch (Exception e)
        {
            Logger.LogError("[DoTransfer] {TableName} sync err : {Error}", tableName, e);
        }
    }

    /// <summary>
    ///     新行同步 : 按自增 Id 水位从本地最大 Id 之后分批拉取远端数据 ,
    ///     批内过滤掉本地已存在的 Id 后插入 , 单条坏数据不会中断整表同步
    /// </summary>
    private static async Task<int> SyncNewRowsById<T>(SqlSugarClient remote, SqlSugarClient local)
        where T : class, ILongIdEntity, IUpdateTimeEntity, new()
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

    /// <summary>
    ///     更新同步 : 按 (update_time, id) 双键水位拉取远端被更新的行 , 按主键更新到本地 ,
    ///     使远端的状态流转 ( 如 spider_news_list 0→3→1 ) 与内容修正能传播到本地 ;
    ///     进度持久化在本地水位表 , 单批失败时下一轮从最近水位重拉 , 天然幂等 ;
    ///     本地不存在的 Id 不做兜底插入 ( 新行由 SyncNewRowsById 负责 , 且可避免两边 Id 错位时产生重复行 )
    /// </summary>
    private static async Task<int> SyncUpdatedRowsByUpdateTime<T>(SqlSugarClient remote, SqlSugarClient local,
        string tableName)
        where T : class, ILongIdEntity, IUpdateTimeEntity, new()
    {
        var watermark = await LoadWatermark(local, tableName);
        // 首次运行无水位 : 以远端当前最大 update_time 为起点 , 只跟踪此后的更新 ( 存量差异不回补 )
        if (watermark == null)
        {
            var maxUpdateTime = await remote.Queryable<T>().MaxAsync(it => it.UpdateTime);
            SaveWatermark(local, tableName, maxUpdateTime, 0);
            Logger.LogInformation("[DoTransfer] {TableName} watermark init at remote max update_time : {MaxTime}",
                tableName, maxUpdateTime);
            return 0;
        }

        var (updateTime, lastId) = watermark.Value;
        var updateNumber = 0;
        while (true)
        {
            // 双键推进 : 同一 update_time 的行被批次截断时 , 依靠 Id 续拉避免漏行
            var cursorTime = updateTime;
            var cursorId = lastId;
            var batch = await remote.Queryable<T>()
                .Where(it => it.UpdateTime > cursorTime || (it.UpdateTime == cursorTime && it.Id > cursorId))
                .OrderBy(it => it.UpdateTime, OrderByType.Asc)
                .OrderBy(it => it.Id, OrderByType.Asc)
                .Take(BatchSize).ToListAsync();
            if (batch.Count == 0) break;

            var batchIds = batch.Select(it => it.Id).ToList();
            var localExistsIds = await local.Queryable<T>()
                .Where(it => batchIds.Contains(it.Id))
                .Select(it => it.Id).ToListAsync();
            var updateItems = batch.Where(it => localExistsIds.Contains(it.Id)).ToList();
            if (updateItems.Count > 0)
            {
                // 忽略时间戳列 , 本地 update_time 由本地触发器自行维护
                updateNumber += await local.Updateable(updateItems)
                    .IgnoreColumns("create_time", "update_time")
                    .ExecuteCommandAsync();
            }

            updateTime = batch[^1].UpdateTime;
            lastId = batch[^1].Id;
            // 每批持久化水位 , 中途失败时下一轮从最近水位续拉
            SaveWatermark(local, tableName, updateTime, lastId);
            if (batch.Count < BatchSize) break;
        }

        return updateNumber;
    }

    /// <summary>
    ///     本地水位表 ( 幂等创建 , 仅存在于本地同步库 ) : 记录每张表已同步到的 (update_time, id) 位置
    /// </summary>
    private static void EnsureWatermarkTable(SqlSugarClient local)
    {
        local.Ado.ExecuteCommand("""
                                 CREATE TABLE IF NOT EXISTS sync_transfer_watermark
                                 (
                                     table_name       text      NOT NULL PRIMARY KEY,
                                     last_update_time timestamp NOT NULL,
                                     last_id          bigint    NOT NULL DEFAULT 0
                                 )
                                 """);
    }

    private static async Task<(DateTime UpdateTime, long Id)?> LoadWatermark(SqlSugarClient local, string tableName)
    {
        var times = await local.Ado.SqlQueryAsync<DateTime>(
            "SELECT last_update_time FROM sync_transfer_watermark WHERE table_name = @tableName",
            new { tableName });
        if (times.Count == 0) return null;

        var ids = await local.Ado.SqlQueryAsync<long>(
            "SELECT last_id FROM sync_transfer_watermark WHERE table_name = @tableName",
            new { tableName });
        return (times[0], ids.Count > 0 ? ids[0] : 0);
    }

    private static void SaveWatermark(SqlSugarClient local, string tableName, DateTime updateTime, long lastId)
    {
        local.Ado.ExecuteCommand(
            """
            INSERT INTO sync_transfer_watermark (table_name, last_update_time, last_id)
            VALUES (@tableName, @updateTime, @lastId)
            ON CONFLICT (table_name) DO UPDATE SET last_update_time = EXCLUDED.last_update_time,
                                                   last_id          = EXCLUDED.last_id
            """,
            new { tableName, updateTime, lastId });
    }
}
