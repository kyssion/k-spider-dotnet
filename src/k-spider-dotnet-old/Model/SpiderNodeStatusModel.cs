using SqlSugar;

namespace KSpider.Model;

/// <summary>
///     节点状态快照 ( NewsCheckJob 每轮 upsert ) : payload 为 JSON 文本 ,
///     含接口探测失败 / 管线积压 / 快讯滞后 / 反爬验证冷却 , Web 控制台解析展示。
///     report_time 距现在过远即视为节点失联。
/// </summary>
[SugarTable("spider_node_status")]
public class SpiderNodeStatusModel
{
    /// <summary>节点标识 ( v1 单节点 = 主机名 )</summary>
    [SugarColumn(IsPrimaryKey = true, ColumnName = "node_id")]
    public string NodeId { get; set; } = "";

    /// <summary>快照上报时间</summary>
    [SugarColumn(ColumnName = "report_time")]
    public DateTime ReportTime { get; set; }

    /// <summary>统计快照 JSON 文本 ( 见 NewsCheckJob.ReportNodeStatus )</summary>
    [SugarColumn(ColumnName = "payload")]
    public string? Payload { get; set; }

    [SugarColumn(ColumnName = "create_time")]
    public DateTime CreateTime { get; set; }

    [SugarColumn(ColumnName = "update_time")]
    public DateTime UpdateTime { get; set; }
}
