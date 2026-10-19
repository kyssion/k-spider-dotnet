using SqlSugar;

namespace KSpider.Model;

/// <summary>
///     任务调度态 ( 每节点每任务一行 , upsert 不膨胀 ) :
///     执行结果列由 JobRuntimeListener 写 , 调度列 ( next_fire_time / is_paused ) 由 NodeStateJob 刷新 ,
///     k-spider-web 控制台读取展示。
/// </summary>
[SugarTable("spider_job_state")]
public class SpiderJobStateModel
{
    /// <summary>节点标识 ( v1 单节点 = 主机名 )</summary>
    [SugarColumn(IsPrimaryKey = true, ColumnName = "node_id")]
    public string NodeId { get; set; } = "";

    /// <summary>任务名 ( Quartz JobKey.Name , 如 NewsListJob )</summary>
    [SugarColumn(IsPrimaryKey = true, ColumnName = "job_name")]
    public string JobName { get; set; } = "";

    /// <summary>下次触发时间 ( 本地时区 ) ; 无触发器时为空</summary>
    [SugarColumn(ColumnName = "next_fire_time")]
    public DateTime? NextFireTime { get; set; }

    /// <summary>是否被暂停</summary>
    [SugarColumn(ColumnName = "is_paused")]
    public bool IsPaused { get; set; }

    /// <summary>上次触发时间 ; 空 = 该任务从未执行</summary>
    [SugarColumn(ColumnName = "last_fired_at")]
    public DateTime? LastFiredAt { get; set; }

    /// <summary>上次执行耗时 ( 毫秒 )</summary>
    [SugarColumn(ColumnName = "last_duration_ms")]
    public long? LastDurationMs { get; set; }

    /// <summary>上次执行是否成功 ; 空 = 从未执行</summary>
    [SugarColumn(ColumnName = "last_success")]
    public bool? LastSuccess { get; set; }

    /// <summary>连续失败次数 ( 成功一次即清零 )</summary>
    [SugarColumn(ColumnName = "consecutive_failures")]
    public int ConsecutiveFailures { get; set; }

    /// <summary>上次执行的异常信息</summary>
    [SugarColumn(ColumnName = "last_error")]
    public string? LastError { get; set; }

    /// <summary>上次执行摘要 ( 如 "新增列表 12" )</summary>
    [SugarColumn(ColumnName = "last_stats")]
    public string? LastStats { get; set; }

    [SugarColumn(ColumnName = "create_time")]
    public DateTime CreateTime { get; set; }

    [SugarColumn(ColumnName = "update_time")]
    public DateTime UpdateTime { get; set; }
}
