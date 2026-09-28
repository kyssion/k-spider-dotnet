using KSpider.Data;
using KSpider.Model;
using SqlSugar;

namespace KSpider.Web.Query;

/// <summary>
///     任务指令服务 : 写入 spider_job_command ( trigger / pause / resume ) , 爬虫节点的 NodeStateJob 轮询消费 ,
///     结果异步回写 ( 前端轮询 /api/jobs/commands 或 /api/status/jobs 看生效 )。
/// </summary>
public class JobCommandService(SystemStatusDao systemStatusDao, Pg pg)
{
    /// <summary>允许的指令类型</summary>
    public static readonly string[] AllowedActions = ["trigger", "pause", "resume"];

    /// <summary>写一条指令并返回受理记录 ( status=pending ) ; node 为空表示任意节点消费</summary>
    public SpiderJobCommandModel Create(string? node, string jobName, string action)
    {
        if (string.IsNullOrWhiteSpace(jobName) || jobName.Length > 100)
            throw new ArgumentException("任务名不能为空且不超过 100 字符");
        node = string.IsNullOrWhiteSpace(node) ? null : node.Trim();
        using var connection = pg.Connection();
        var id = systemStatusDao.InsertJobCommand(connection, node, jobName.Trim(), action);
        return new SpiderJobCommandModel
        {
            Id = id, NodeId = node, JobName = jobName.Trim(), Action = action, Status = "pending"
        };
    }

    /// <summary>最近的指令记录 ( 含消费结果 )</summary>
    public List<SpiderJobCommandModel> Recent(int limit)
    {
        limit = Math.Clamp(limit, 1, 100);
        using var connection = pg.Connection();
        return connection.Queryable<SpiderJobCommandModel>()
            .OrderByDescending(it => it.Id).Take(limit).ToList();
    }
}
