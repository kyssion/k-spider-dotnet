using KSpider.Exceptions;
using KSpider.Model;
using SqlSugar;

namespace KSpider.Data;

/// <summary>
///     盘面榜单 DAO ( Ranking 管线 spider_ranking 通道 , DI Singleton ) :
///     批量 upsert ( 手拼 ON CONFLICT DO NOTHING , 发布即终态 ) + 已存在行键查询 ( 翻页停止判定 )。
/// </summary>
public class SpiderRankingDao
{
    /// <summary>
    ///     榜单批量写入 : ( ranking_type, trade_date, row_key ) 冲突 DO NOTHING ( 快照发布即终态 )
    /// </summary>
    public int UpsertRankingOnConflict(SqlSugarClient connection,
        List<SpiderRankingModel> rows, int maxBatchNumber)
    {
        try
        {
            rows = rows.GroupBy(item => $"{item.RankingType}|{item.TradeDate:yyyy-MM-dd}|{item.RowKey}")
                .Select(item => item.First()).ToList();
            if (rows.Count == 0) return 0;
            if (rows.Count > maxBatchNumber)
            {
                var allNumber = 0;
                for (var i = 0; i < rows.Count; i += maxBatchNumber)
                    allNumber += UpsertRankingOnConflict(connection,
                        rows.GetRange(i, Math.Min(maxBatchNumber, rows.Count - i)), maxBatchNumber);
                return allNumber;
            }

            var item2 = connection.Insertable(rows).IgnoreColumns("id", "create_time", "update_time");
            item2.InsertBuilder.IsNoPage = true;
            var insertSql = SpiderNewsBatchDao.TrimInsertSqlTail(item2.ToSqlString());
            var sqlTemple = $"""
                             {insertSql}
                             ON CONFLICT (ranking_type, trade_date, row_key) DO NOTHING;
                             """;
            return connection.Ado.ExecuteCommand(sqlTemple);
        }
        catch (Exception e)
        {
            throw new KDbException($"[UpsertRankingOnConflict] err : {e}", e);
        }
    }

    /// <summary>
    ///     查已存在的行键集合 ( 判定"当前页已全部存在即停" ) , 详情表 detail 列不含在查询里
    /// </summary>
    public HashSet<string> GetExistsRowKeys(SqlSugarClient connection, short rankingType, DateTime tradeDate,
        List<string> rowKeys)
    {
        try
        {
            return connection.Queryable<SpiderRankingModel>()
                .Where(it => it.RankingType == rankingType && it.TradeDate == tradeDate &&
                             rowKeys.Contains(it.RowKey))
                .Select(it => it.RowKey)
                .ToList()
                .ToHashSet();
        }
        catch (Exception e)
        {
            throw new KDbException($"[GetExistsRowKeys] err : {e}", e);
        }
    }
}
