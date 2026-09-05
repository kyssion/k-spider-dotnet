using k_spider_dotnet_lib.logger;
using k_spider_dotnet.exception;
using k_spider_dotnet.model;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace k_spider_dotnet.data;

public class StockDao
{
    private static readonly ILogger Log = LogFactory.GetLogger<StockDao>();

    public static int UpsetStockCnIntroduction(SqlSugarClient connection, List<StockCnIntroductionModel> stockLists)
    {
        try
        {
            var storageAble = connection.Storageable(stockLists).WhereColumns(it => it.StockId).ToStorage();
            return storageAble.AsInsertable.IgnoreColumns("id", "create_time", "update_time").ExecuteCommand() +
                   storageAble.AsUpdateable.IgnoreColumns("id", "create_time", "update_time").ExecuteCommand();
        }
        catch (Exception e)
        {
            throw new KDbException("[UpsetSpiderNewsContentOrigin] err : {e}", e);
        }
    }

    public static int BatchUpsetCnLevel1ArchivedDaily(SqlSugarClient connection,
        List<StockCnLevel1ArchivedDailyOriginModel> stockCnLevelModels)
    {
        try
        {
            var storageAble = connection.Storageable(stockCnLevelModels).WhereColumns(it => new {it.StockId, it.Date}).ToStorage();
            return storageAble.AsInsertable.IgnoreColumns("id", "create_time", "update_time").ExecuteCommand() +
                   storageAble.AsUpdateable.IgnoreColumns("id", "create_time", "update_time").ExecuteCommand();
        }
        catch (Exception e)
        {
            throw new KDbException("[UpsetSpiderNewsContentOrigin] err : {e}", e);
        }
    }
    public static int UpsetCnLevel1ArchivedDaily(SqlSugarClient connection,
        StockCnLevel1ArchivedDailyOriginModel stockCnLevelModel)
    {
        try
        {
            var storageAble = connection.Storageable(stockCnLevelModel).WhereColumns(it => new {it.StockId, it.Date}).ToStorage();
            return storageAble.AsInsertable.IgnoreColumns("id", "create_time", "update_time").ExecuteCommand() +
                   storageAble.AsUpdateable.IgnoreColumns("id", "create_time", "update_time").ExecuteCommand();
        }
        catch (Exception e)
        {
            throw new KDbException("[UpsetSpiderNewsContentOrigin] err : {e}", e);
        }
    }
    
    
    public static int BatchUpsetHkLevel1ArchivedDaily(SqlSugarClient connection,
        List<StockHkLevel1ArchivedDailyOriginModel> stockCnLevelModels)
    {
        try
        {
            var storageAble = connection.Storageable(stockCnLevelModels).WhereColumns(it => new {it.StockId, it.Date}).ToStorage();
            return storageAble.AsInsertable.IgnoreColumns("id", "create_time", "update_time").ExecuteCommand() +
                   storageAble.AsUpdateable.IgnoreColumns("id", "create_time", "update_time").ExecuteCommand();
        }
        catch (Exception e)
        {
            throw new KDbException("[UpsetSpiderNewsContentOrigin] err : {e}", e);
        }
    }
    public static int UpsetHkLevel1ArchivedDaily(SqlSugarClient connection,
        StockHkLevel1ArchivedDailyOriginModel stockCnLevelModel)
    {
        try
        {
            var storageAble = connection.Storageable(stockCnLevelModel).WhereColumns(it => new {it.StockId, it.Date}).ToStorage();
            return storageAble.AsInsertable.IgnoreColumns("id", "create_time", "update_time").ExecuteCommand() +
                   storageAble.AsUpdateable.IgnoreColumns("id", "create_time", "update_time").ExecuteCommand();
        }
        catch (Exception e)
        {
            throw new KDbException("[UpsetSpiderNewsContentOrigin] err : {e}", e);
        }
    }
}