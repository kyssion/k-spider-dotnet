using KSpider.Web.Query;
using Microsoft.AspNetCore.Builder;

namespace KSpider.Web.Api;

/// <summary>
///     任务控制端点 : /api/jobs/* —— 异步受理 ( 写指令表 ) , 爬虫节点 NodeStateJob 轮询消费 ( 约 3 秒内生效 )
/// </summary>
public static class JobCommandEndpoints
{
    public static void MapJobCommandApis(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/jobs").WithTags("任务控制");

        // 手动触发 / 暂停 / 恢复 : 202 受理 , 结果看指令历史或任务状态
        group.MapPost("/{jobName}/{action}", (string jobName, string action, JobCommandService service) =>
        {
            if (!JobCommandService.AllowedActions.Contains(action))
                return Results.BadRequest(new
                {
                    error = $"未知指令 : {action} , 允许 {string.Join(" / ", JobCommandService.AllowedActions)}"
                });
            try
            {
                var command = service.Create(null, jobName, action);
                return Results.Accepted(value: command);
            }
            catch (ArgumentException e)
            {
                return Results.BadRequest(new { error = e.Message });
            }
        });

        // 最近指令记录 ( 含消费结果 )
        group.MapGet("/commands", (JobCommandService service, int? limit) => service.Recent(limit ?? 50));
    }
}
