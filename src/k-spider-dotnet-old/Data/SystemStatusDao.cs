using KSpider.Exceptions;
using KSpider.Model;
using SqlSugar;

namespace KSpider.Data;

/// <summary>
///     系统状态 DAO : Web 控制台 ( k-spider-web ) 的跨进程通道——主程序写任务调度态 / 消费指令 / 上报节点快照 ,
///     Web 端读状态 / 写指令。建表见 db/k_script_spider.sql 与 Pg.EnsureSystemDbObjects。
///     写入用参数化 SQL 而非 Storageable : 执行列与调度列由不同写者各管各的 , 列选择必须显式 ,
///     连续失败计数也靠 ON CONFLICT 里的 CASE 原子累计 ( 免读改写竞态 )。
/// </summary>
public class SystemStatusDao
{
    /// <summary>
    ///     执行结果上报 ( JobRuntimeListener 每次任务执行后调用 ) :
    ///     新行插入执行列 , 已有行只更新执行列。
    /// </summary>
    public int UpsertJobExecution(SqlSugarClient connection, JobExecutionReport report)
    {
        try
        {
            // 参数用匿名对象平铺 ( SqlSugar 的 ExecuteCommand 只认匿名对象 / SugarParameter , 直接传 record 会报参数格式错误 )
            return connection.Ado.ExecuteCommand("""
                INSERT INTO spider_job_state
                    (node_id, job_name, last_fired_at, last_duration_ms, last_success,
                     consecutive_failures, last_error, last_stats, next_fire_time)
                VALUES (@NodeId, @JobName, @FiredAt, @DurationMs, @Success,
                        CASE WHEN @Success THEN 0 ELSE 1 END, @Error, @Stats, @NextFireTime)
                ON CONFLICT (node_id, job_name) DO UPDATE SET
                    last_fired_at        = EXCLUDED.last_fired_at,
                    last_duration_ms     = EXCLUDED.last_duration_ms,
                    last_success         = EXCLUDED.last_success,
                    consecutive_failures = CASE WHEN EXCLUDED.last_success THEN 0
                                                 ELSE spider_job_state.consecutive_failures + 1 END,
                    last_error           = EXCLUDED.last_error,
                    last_stats           = EXCLUDED.last_stats
                """, new
            {
                report.NodeId,
                report.JobName,
                report.FiredAt,
                report.DurationMs,
                report.Success,
                report.Error,
                report.Stats,
                report.NextFireTime
            });
        }
        catch (Exception e)
        {
            throw new KDbException($"[UpsertJobExecution] err : {e}", e);
        }
    }

    /// <summary>调度态上报 ( NodeStateJob 每 3 秒刷新 ) : 只动下次触发时间与暂停状态两列</summary>
    public int UpsertJobSchedule(SqlSugarClient connection, string nodeId, string jobName,
        DateTime? nextFireTime, bool isPaused)
    {
        try
        {
            return connection.Ado.ExecuteCommand("""
                INSERT INTO spider_job_state (node_id, job_name, next_fire_time, is_paused)
                VALUES (@nodeId, @jobName, @nextFireTime, @isPaused)
                ON CONFLICT (node_id, job_name) DO UPDATE SET
                    next_fire_time = EXCLUDED.next_fire_time,
                    is_paused      = EXCLUDED.is_paused
                """, new { nodeId, jobName, nextFireTime, isPaused });
        }
        catch (Exception e)
        {
            throw new KDbException($"[UpsertJobSchedule] err : {e}", e);
        }
    }

    /// <summary>写一条任务指令 ( k-spider-web 的手动触发 / 暂停 / 恢复 ) , 返回指令 id</summary>
    public long InsertJobCommand(SqlSugarClient connection, string? nodeId, string jobName, string action)
    {
        try
        {
            return connection.Ado.SqlQuery<long>("""
                INSERT INTO spider_job_command (node_id, job_name, action)
                VALUES (@nodeId, @jobName, @action)
                RETURNING id
                """, new { nodeId, jobName, action }).First();
        }
        catch (Exception e)
        {
            throw new KDbException($"[InsertJobCommand] err : {e}", e);
        }
    }

    /// <summary>待消费指令 : pending 且未指定节点或指定本节点 , 按写入顺序</summary>
    public List<SpiderJobCommandModel> PendingJobCommands(SqlSugarClient connection, string nodeId)
    {
        try
        {
            return connection.Queryable<SpiderJobCommandModel>()
                .Where(it => it.Status == "pending" && (it.NodeId == null || it.NodeId == "" || it.NodeId == nodeId))
                .OrderBy(it => it.Id)
                .ToList();
        }
        catch (Exception e)
        {
            throw new KDbException($"[PendingJobCommands] err : {e}", e);
        }
    }

    /// <summary>回写指令执行结果 ( status : done / rejected )</summary>
    public int FinishJobCommand(SqlSugarClient connection, long id, string status, string result)
    {
        try
        {
            return connection.Ado.ExecuteCommand(
                "UPDATE spider_job_command SET status = @status, result = @result, consumed_at = @consumedAt WHERE id = @id",
                new { id, status, result, consumedAt = DateTime.Now });
        }
        catch (Exception e)
        {
            throw new KDbException($"[FinishJobCommand] err : {e}", e);
        }
    }

    /// <summary>节点状态快照 upsert ( NewsCheckJob 每轮 ) : payload 为 JSON 文本</summary>
    public int UpsertNodeStatus(SqlSugarClient connection, string nodeId, string payload)
    {
        try
        {
            return connection.Ado.ExecuteCommand("""
                INSERT INTO spider_node_status (node_id, report_time, payload)
                VALUES (@nodeId, CURRENT_TIMESTAMP, @payload)
                ON CONFLICT (node_id) DO UPDATE SET
                    report_time = EXCLUDED.report_time,
                    payload     = EXCLUDED.payload
                """, new { nodeId, payload });
        }
        catch (Exception e)
        {
            throw new KDbException($"[UpsertNodeStatus] err : {e}", e);
        }
    }
}

/// <summary>单次任务执行的上报数据 ( JobRuntimeListener → spider_job_state )</summary>
public sealed record JobExecutionReport(
    string NodeId,
    string JobName,
    DateTime FiredAt,
    long DurationMs,
    bool Success,
    string? Error,
    string? Stats,
    DateTime? NextFireTime);
