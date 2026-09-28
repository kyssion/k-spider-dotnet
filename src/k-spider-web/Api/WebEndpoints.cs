using Microsoft.AspNetCore.Builder;

namespace KSpider.Web.Api;

/// <summary>
///     API 装配入口 : 全部端点挂在 /api 下 , 新增域在这里加一行
/// </summary>
public static class WebEndpoints
{
    public static void MapSpiderApis(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        // 存活探针 ( nginx / 部署检查用 )
        api.MapGet("/health", () => Results.Ok(new { ok = true, time = DateTime.Now }))
            .WithTags("运行状态").AllowAnonymous();

        api.MapStatusApis();
        api.MapNewsApis();
        api.MapFlashApis();
        api.MapAnalysisApis();
        api.MapJobCommandApis();
    }
}
