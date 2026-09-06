using KSpider.Logger;
using KSpider.Exceptions;
using KSpider.Model;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace KSpider.Data;

public class StockDao
{

    public int UpsetStockCnIntroduction(SqlSugarClient connection, List<StockCnIntroductionModel> stockLists)
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

    public int BatchUpsetCnLevel1ArchivedDaily(SqlSugarClient connection,
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
    public int UpsetCnLevel1ArchivedDaily(SqlSugarClient connection,
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
    
    
    public int BatchUpsetHkLevel1ArchivedDaily(SqlSugarClient connection,
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
    public int UpsetHkLevel1ArchivedDaily(SqlSugarClient connection,
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