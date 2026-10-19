using KSpider.Exceptions;
using KSpider.Model;
using SqlSugar;

namespace KSpider.Data;

/// <summary>
///     官方文件 DAO ( Policy 管线 spider_policy_document 通道 , DI Singleton ) :
///     批量 upsert ( ON CONFLICT DO NOTHING , 发布即终态 ) + 已存在键查询 ( 翻页停止判定 )。
/// </summary>
public class SpiderPolicyDao
{
    /// <summary>
    ///     文档批量写入 : ( from_media, doc_id ) 冲突 DO NOTHING
    /// </summary>
    public int UpsertPolicyDocumentOnConflict(SqlSugarClient connection,
        List<SpiderPolicyDocumentModel> documents, int maxBatchNumber)
    {
        try
        {
            documents = documents.GroupBy(item => $"{item.FromMedia}|{item.DocId}")
                .Select(item => item.First()).ToList();
            if (documents.Count == 0) return 0;
            if (documents.Count > maxBatchNumber)
            {
                var allNumber = 0;
                for (var i = 0; i < documents.Count; i += maxBatchNumber)
                    allNumber += UpsertPolicyDocumentOnConflict(connection,
                        documents.GetRange(i, Math.Min(maxBatchNumber, documents.Count - i)), maxBatchNumber);
                return allNumber;
            }

            var item = connection.Insertable(documents).IgnoreColumns("id", "create_time", "update_time");
            item.InsertBuilder.IsNoPage = true;
            var insertSql = SpiderNewsBatchDao.TrimInsertSqlTail(item.ToSqlString());
            var sqlTemple = $"""
                             {insertSql}
                             ON CONFLICT (from_media, doc_id) DO NOTHING;
                             """;
            return connection.Ado.ExecuteCommand(sqlTemple);
        }
        catch (Exception e)
        {
            throw new KDbException($"[UpsertPolicyDocumentOnConflict] err : {e}", e);
        }
    }

    /// <summary>
    ///     查已存在的文档键集合 ( 判定"当前页已全部存在即停" )
    /// </summary>
    public HashSet<string> GetExistsDocIds(SqlSugarClient connection, int fromMedia, List<string> docIds)
    {
        try
        {
            return connection.Queryable<SpiderPolicyDocumentModel>()
                .Where(it => it.FromMedia == fromMedia && docIds.Contains(it.DocId))
                .Select(it => it.DocId)
                .ToList()
                .ToHashSet();
        }
        catch (Exception e)
        {
            throw new KDbException($"[GetExistsDocIds] err : {e}", e);
        }
    }
}
