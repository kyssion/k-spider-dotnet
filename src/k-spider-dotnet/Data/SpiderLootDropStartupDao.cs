using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider;
using SqlSugar;

namespace KSpider.Data;

/// <summary>
///     Loot Drop 数据集 DAO ( 第七管线 spider_lootdrop_startup 通道 , DI Singleton ) :
///     全量批量 upsert —— 与其它管线的本质差异在冲突子句 :
///     ON CONFLICT (id) DO UPDATE ... WHERE content_hash 变化 才 UPDATE ,
///     新行插入与修订行更新是同一条语句 , 内容没变的行零写入
///     ( update_time 触发器只在真变更时刷新 , 保住"最后修改时间"语义 )。
///     事务由 Job 层管理 ; 手拼冲突子句与 SpiderNewsBatchDao 同款实现。
/// </summary>
public class SpiderLootDropStartupDao
{
    /// <summary>
    ///     冲突时的更新列 : 全部内容列 + content_hash + raw_content ;
    ///     不含 id / from_media ( 源身份不可变 ) 与 create_time / update_time ( 库管理列 , 触发器维护 )
    /// </summary>
    private const string OnConflictUpdateColumns = """
        name = EXCLUDED.name,
        description = EXCLUDED.description,
        sector = EXCLUDED.sector,
        country = EXCLUDED.country,
        founders = EXCLUDED.founders,
        investors = EXCLUDED.investors,
        start_year = EXCLUDED.start_year,
        end_year = EXCLUDED.end_year,
        total_funding = EXCLUDED.total_funding,
        difficulty = EXCLUDED.difficulty,
        difficulty_reason = EXCLUDED.difficulty_reason,
        scalability = EXCLUDED.scalability,
        scalability_reason = EXCLUDED.scalability_reason,
        market_potential = EXCLUDED.market_potential,
        market_potential_reason = EXCLUDED.market_potential_reason,
        cause_of_death = EXCLUDED.cause_of_death,
        the_loot = EXCLUDED.the_loot,
        market_analysis = EXCLUDED.market_analysis,
        pivot_idea = EXCLUDED.pivot_idea,
        source_created_at = EXCLUDED.source_created_at,
        status = EXCLUDED.status,
        primary_cause_of_death = EXCLUDED.primary_cause_of_death,
        product_type = EXCLUDED.product_type,
        views = EXCLUDED.views,
        condensed_value_prop = EXCLUDED.condensed_value_prop,
        condensed_cause_of_death = EXCLUDED.condensed_cause_of_death,
        content_hash = EXCLUDED.content_hash,
        raw_content = EXCLUDED.raw_content
        """;

    /// <summary>
    ///     数据集批量 upsert : 返回受影响行数 ( 新插入 + 真正修订的行 ;
    ///     内容没变的行不产生写入 , 计数不含 )。
    ///     返回值恒非负 , 供 RunSummary 与对账口径使用。
    /// </summary>
    public int UpsertLootDropStartups(SqlSugarClient connection,
        List<SpiderLootDropStartupModel> startups, int maxBatchNumber)
    {
        try
        {
            startups = startups.GroupBy(item => item.Id).Select(item => item.First()).ToList();
            if (startups.Count == 0) return 0;
            if (startups.Count > maxBatchNumber)
            {
                var allNumber = 0;
                for (var i = 0; i < startups.Count; i += maxBatchNumber)
                    allNumber += UpsertLootDropStartups(connection,
                        startups.GetRange(i, Math.Min(maxBatchNumber, startups.Count - i)), maxBatchNumber);
                return allNumber;
            }

            // IsNoPage : 绕过 ToSqlString 默认 200 行自动分页 , 整批生成一条 INSERT 再手拼冲突子句 ;
            // create_time/update_time 交给库默认值与触发器 , id 必须写入 ( 源侧主键非自增 )
            var item = connection.Insertable(startups).IgnoreColumns("create_time", "update_time");
            item.InsertBuilder.IsNoPage = true;
            var insertSql = SpiderNewsBatchDao.TrimInsertSqlTail(item.ToSqlString());
            return connection.Ado.ExecuteCommand(BuildUpsertSql(insertSql));
        }
        catch (Exception e)
        {
            throw new KDbException($"[UpsertLootDropStartups] err : {e}", e);
        }
    }

    /// <summary>
    ///     拼接最终 upsert 语句 ( 独立成静态方法供离线形态锁测试 ) :
    ///     ON CONFLICT (id) DO UPDATE ... WHERE content_hash 变化 —— 新行插入与修订行更新同一条语句,
    ///     内容没变的行零写入 ( 受影响行数不含它们 )
    /// </summary>
    public static string BuildUpsertSql(string insertSql)
    {
        return $"""

                 {insertSql}
                 ON CONFLICT (id) DO UPDATE SET
                 {OnConflictUpdateColumns}
                 WHERE spider_lootdrop_startup.content_hash IS DISTINCT FROM EXCLUDED.content_hash;
                 """;
    }

    /// <summary>本地行数 ( 对账监控用 )</summary>
    public long GetStartupCount(SqlSugarClient connection)
    {
        try
        {
            return connection.Queryable<SpiderLootDropStartupModel>().Count();
        }
        catch (Exception e)
        {
            throw new KDbException($"[GetStartupCount] err : {e}", e);
        }
    }
}
