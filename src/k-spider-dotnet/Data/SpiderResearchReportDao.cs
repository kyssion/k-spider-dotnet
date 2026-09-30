using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider;
using SqlSugar;

namespace KSpider.Data;

/// <summary>
///     研报 DAO ( 第三管线 spider_research_report 通道 , DI Singleton ) :
///     列表批量 upsert ( 手拼 ON CONFLICT , 与 SpiderNewsBatchDao 同款实现 ) + 摘要回填三件套。
///     事务由 Job 层管理 ; 本管线无状态机 , 摘要回填失败以 summary_fail_count 计数。
/// </summary>
public class SpiderResearchReportDao
{
    /// <summary>
    ///     研报批量写入 : info_code 冲突 DO NOTHING ( 研报元数据发布后不可变 ,
    ///     重复拉回的时间窗条目不产生空转 UPDATE , update_time 保持"最后修改时间"语义 )
    /// </summary>
    public int UpsertResearchReportOnConflict(SqlSugarClient connection,
        List<SpiderResearchReportModel> reports, int maxBatchNumber)
    {
        try
        {
            reports = reports.GroupBy(item => item.InfoCode).Select(item => item.First()).ToList();
            if (reports.Count == 0) return 0;
            if (reports.Count > maxBatchNumber)
            {
                var allNumber = 0;
                for (var i = 0; i < reports.Count; i += maxBatchNumber)
                    allNumber += UpsertResearchReportOnConflict(connection,
                        reports.GetRange(i, Math.Min(maxBatchNumber, reports.Count - i)), maxBatchNumber);
                return allNumber;
            }

            // IsNoPage : 绕过 ToSqlString 默认 200 行自动分页 , 整批生成一条 INSERT 再手拼 ON CONFLICT
            var item = connection.Insertable(reports).IgnoreColumns("id", "create_time", "update_time");
            item.InsertBuilder.IsNoPage = true;
            var insertSql = SpiderNewsBatchDao.TrimInsertSqlTail(item.ToSqlString());
            var sqlTemple = $"""
                             {insertSql}
                             ON CONFLICT (info_code) DO NOTHING;
                             """;
            return connection.Ado.ExecuteCommand(sqlTemple);
        }
        catch (Exception e)
        {
            throw new KDbException($"[UpsertResearchReportOnConflict] err : {e}", e);
        }
    }

    /// <summary>
    ///     待回填摘要的研报 : summary 为空且失败未达上限 , 按 Id 先进先出 ,
    ///     部分索引 idx_research_report_pending_summary 支撑 ( 每 5 分钟限量一批 )
    /// </summary>
    public List<SpiderResearchReportModel> GetPendingSummaryReports(SqlSugarClient connection, int limit)
    {
        try
        {
            return connection.Queryable<SpiderResearchReportModel>()
                .Where(it => it.Summary == null &&
                             it.SummaryFailCount < NewsPipelineConst.MaxFailCount)
                .OrderBy(it => it.Id)
                .Take(limit)
                .ToList();
        }
        catch (Exception e)
        {
            throw new KDbException($"[GetPendingSummaryReports] err : {e}", e);
        }
    }

    /// <summary>
    ///     回填摘要正文 ( 单行 , 无事务需求 )
    /// </summary>
    public int FillSummary(SqlSugarClient connection, long id, string summary)
    {
        try
        {
            return connection.Ado.ExecuteCommand(
                "UPDATE public.spider_research_report SET summary = @summary WHERE id = @id",
                new { id, summary });
        }
        catch (Exception e)
        {
            throw new KDbException($"[FillSummary] err : {e}", e);
        }
    }

    /// <summary>
    ///     摘要回填失败计数 +1 ( 达上限后不再被部分索引命中 , 该行停用重试 )
    /// </summary>
    public int IncreaseSummaryFailCount(SqlSugarClient connection, long id)
    {
        try
        {
            return connection.Ado.ExecuteCommand(
                "UPDATE public.spider_research_report SET summary_fail_count = summary_fail_count + 1 WHERE id = @id",
                new { id });
        }
        catch (Exception e)
        {
            throw new KDbException($"[IncreaseSummaryFailCount] err : {e}", e);
        }
    }
}
