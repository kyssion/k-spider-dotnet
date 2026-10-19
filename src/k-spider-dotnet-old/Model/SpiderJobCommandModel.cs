using SqlSugar;

namespace KSpider.Model;

/// <summary>
///     任务指令 : k-spider-web 写入 ( 手动触发 / 暂停 / 恢复 ) , 爬虫节点由 NodeStateJob 轮询消费后回写结果。
///     这是跨进程控制通道 , 也是将来分布式任务分发表的最小形态。
/// </summary>
[SugarTable("spider_job_command")]
public class SpiderJobCommandModel
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
    public long Id { get; set; }

    /// <summary>目标节点 ; 空 = 任意节点消费 ( v1 单节点都用空 )</summary>
    [SugarColumn(ColumnName = "node_id")]
    public string? NodeId { get; set; }

    /// <summary>任务名 ( Quartz JobKey.Name )</summary>
    [SugarColumn(ColumnName = "job_name")]
    public string JobName { get; set; } = "";

    /// <summary>指令类型 : trigger / pause / resume</summary>
    [SugarColumn(ColumnName = "action")]
    public string Action { get; set; } = "";

    /// <summary>状态 : pending 待消费 / done 已执行 / rejected 已拒绝</summary>
    [SugarColumn(ColumnName = "status")]
    public string Status { get; set; } = "pending";

    /// <summary>执行结果说明 ( 成功反馈或拒绝原因 )</summary>
    [SugarColumn(ColumnName = "result")]
    public string? Result { get; set; }

    /// <summary>消费时间 ; 空 = 尚未消费</summary>
    [SugarColumn(ColumnName = "consumed_at")]
    public DateTime? ConsumedAt { get; set; }

    [SugarColumn(ColumnName = "create_time")]
    public DateTime CreateTime { get; set; }

    [SugarColumn(ColumnName = "update_time")]
    public DateTime UpdateTime { get; set; }
}
