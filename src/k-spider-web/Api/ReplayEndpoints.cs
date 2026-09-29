using KSpider.Model;
using KSpider.Spider.News.Web;
using KSpider.Web.Replay;
using Microsoft.AspNetCore.Builder;

namespace KSpider.Web.Api;

/// <summary>
///     数据重放端点 : /api/replay/* —— origin 重新解析生成 content 的配置入口与进度查询。
///     执行在 Web 进程内异步进行 ( OriginReplayService ) , 同一时刻仅一个任务。
/// </summary>
public static class ReplayEndpoints
{
    public static void MapReplayApis(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/replay").WithTags("数据重放");

        // 可选解析器清单 ( 各源注册表声明的码 )
        group.MapGet("/parsers", () => NewsSpiderRegistry.All.Select(spider => new
        {
            code = spider.ParserCode,
            fromMedia = (int)spider.FromMedia,
            sourceName = spider.FromMedia.ToString(),
        }));

        // 预览 : 命中筛选的 origin 行数 ( 不执行 )
        group.MapPost("/preview", (OriginReplayService service, ReplayRequest request) =>
            Results.Ok(new { count = service.Preview(request.ToFilter()) }));

        // 提交重放任务 : 202 受理 , 进度看 tasks ; 409 = 已有任务在跑
        group.MapPost("/", (OriginReplayService service, ReplayRequest request) =>
        {
            SpiderReplayLogModel task;
            try
            {
                task = service.Submit(request.ToFilter());
            }
            catch (InvalidOperationException e)
            {
                return Results.Conflict(new { error = e.Message });
            }

            return Results.Accepted(value: task);
        });

        // 最近任务 ( 含进行中的进度 )
        group.MapGet("/tasks", (OriginReplayService service, int? limit) => service.Recent(limit ?? 20));
    }
}

/// <summary>重放请求体 ( 页面配置 )</summary>
public sealed record ReplayRequest(
    int? FromMedia,
    string? ParserCode,
    DateTime? Start,
    DateTime? End,
    int? MaxCount)
{
    public ReplayFilter ToFilter()
    {
        return new ReplayFilter(
            FromMedia,
            string.IsNullOrWhiteSpace(ParserCode) ? null : ParserCode.Trim(),
            Start,
            End,
            MaxCount ?? 20000);
    }
}
