using KSpider.Exceptions;
using KSpider.Model;
using SqlSugar;

namespace KSpider.Data;

/// <summary>
///     公告 DAO ( Announcement 管线 spider_announcement 通道 , DI Singleton ) :
///     批量 upsert ( ON CONFLICT DO NOTHING , 披露即终态 ) + 已存在键查询 ( 翻页停止判定 )。
/// </summary>
public class SpiderAnnouncementDao
{
    /// <summary>
    ///     公告批量写入 : announcement_id 冲突 DO NOTHING
    /// </summary>
    public int UpsertAnnouncementOnConflict(SqlSugarClient connection,
        List<SpiderAnnouncementModel> announcements, int maxBatchNumber)
    {
        try
        {
            announcements = announcements.GroupBy(item => item.AnnouncementId)
                .Select(item => item.First()).ToList();
            if (announcements.Count == 0) return 0;
            if (announcements.Count > maxBatchNumber)
            {
                var allNumber = 0;
                for (var i = 0; i < announcements.Count; i += maxBatchNumber)
                    allNumber += UpsertAnnouncementOnConflict(connection,
                        announcements.GetRange(i, Math.Min(maxBatchNumber, announcements.Count - i)), maxBatchNumber);
                return allNumber;
            }

            var item = connection.Insertable(announcements).IgnoreColumns("id", "create_time", "update_time");
            item.InsertBuilder.IsNoPage = true;
            var insertSql = SpiderNewsBatchDao.TrimInsertSqlTail(item.ToSqlString());
            var sqlTemple = $"""
                             {insertSql}
                             ON CONFLICT (announcement_id) DO NOTHING;
                             """;
            return connection.Ado.ExecuteCommand(sqlTemple);
        }
        catch (Exception e)
        {
            throw new KDbException($"[UpsertAnnouncementOnConflict] err : {e}", e);
        }
    }

    /// <summary>
    ///     查已存在的公告键集合 ( 判定"当前页已全部存在即停" )
    /// </summary>
    public HashSet<string> GetExistsIds(SqlSugarClient connection, List<string> announcementIds)
    {
        try
        {
            return connection.Queryable<SpiderAnnouncementModel>()
                .Where(it => announcementIds.Contains(it.AnnouncementId))
                .Select(it => it.AnnouncementId)
                .ToList()
                .ToHashSet();
        }
        catch (Exception e)
        {
            throw new KDbException($"[GetExistsIds] err : {e}", e);
        }
    }
}
