using SqlSugar;

namespace KSpider.Model;

/// <summary>
///     数据重放任务记录 ( k-spider-web 重放工具发起 : origin 重新解析生成 content 的筛选条件 / 进度 / 结果 )。
///     Web 进程内异步执行 , 同一时刻只允许一个任务在跑。
/// </summary>
[SugarTable("spider_replay_log")]
public class SpiderReplayLogModel : ILongIdEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
    public long Id { get; set; }

    /// <summary>筛选条件 JSON 留档 ( 页面配置的源/解析器/时间窗/上限 )</summary>
    [SugarColumn(ColumnName = "filter")]
    public string? Filter { get; set; }

    /// <summary>状态 : running 运行中 / done 完成 / failed 失败中止</summary>
    [SugarColumn(ColumnName = "status")]
    public string Status { get; set; } = "running";

    /// <summary>已处理条数</summary>
    [SugarColumn(ColumnName = "total")]
    public int Total { get; set; }

    /// <summary>解析并写回成功条数</summary>
    [SugarColumn(ColumnName = "success_count")]
    public int SuccessCount { get; set; }

    /// <summary>解析失败条数 ( 含路由不到解析器的行 )</summary>
    [SugarColumn(ColumnName = "fail_count")]
    public int FailCount { get; set; }

    /// <summary>失败摘要 / 中止原因</summary>
    [SugarColumn(ColumnName = "message")]
    public string? Message { get; set; }

    [SugarColumn(ColumnName = "create_time")]
    public DateTime CreateTime { get; set; }

    [SugarColumn(ColumnName = "update_time")]
    public DateTime UpdateTime { get; set; }

    /// <summary>结束时间 ; 空 = 还在跑</summary>
    [SugarColumn(ColumnName = "finish_time")]
    public DateTime? FinishTime { get; set; }
}
