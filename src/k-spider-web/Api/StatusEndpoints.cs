using KSpider.Web.Query;
using Microsoft.AspNetCore.Builder;

namespace KSpider.Web.Api;

/// <summary>
///     运行状态端点 : /api/status/*
/// </summary>
public static class StatusEndpoints
{
    public static void MapStatusApis(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/status").WithTags("运行状态");

        // 全部任务 : 调度态 + 最近执行结果 + 连续失败 ( 前端按节点分组 )
        group.MapGet("/jobs", (StatusQueryService query) => query.JobStates());

        // 网页管线积压 : 源 × 状态计数 + 最老待处理时间
        group.MapGet("/pipeline", (StatusQueryService query) => query.Pipeline());

        // 快讯源实时性 : 最新一条距今
        group.MapGet("/flash-lag", (StatusQueryService query) => query.FlashLag());

        // 节点快照 : payload JSON 原文 + 上报时间 ( 过远即节点失联 )
        group.MapGet("/nodes", (StatusQueryService query) => query.Nodes());
    }
}
