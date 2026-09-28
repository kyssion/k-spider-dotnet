using KSpider.Web.Query;
using Microsoft.AspNetCore.Builder;

namespace KSpider.Web.Api;

/// <summary>
///     数据分析端点 : /api/analysis/* ( 全部强制时间窗 ≤ 31 天 )
/// </summary>
public static class AnalysisEndpoints
{
    public static void MapAnalysisApis(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/analysis").WithTags("数据分析");

        // 入库量趋势 : ?start=&end=&granularity=hour|day ( 默认最近 7 天按天 )
        group.MapGet("/volume", (AnalysisQueryService query, DateTime? start, DateTime? end,
            string? granularity) =>
        {
            var (startAt, endAt) = DefaultWindow(start, end);
            try
            {
                return Results.Ok(query.Volume(startAt, endAt, granularity ?? "day"));
            }
            catch (ArgumentException e)
            {
                return Results.BadRequest(new { error = e.Message });
            }
        });

        // 分布 : 网页新闻按源 / 栏目 , 快讯按源 / 重要度
        group.MapGet("/distribute", (AnalysisQueryService query, DateTime? start, DateTime? end) =>
        {
            var (startAt, endAt) = DefaultWindow(start, end);
            try
            {
                return Results.Ok(query.Distribute(startAt, endAt));
            }
            catch (ArgumentException e)
            {
                return Results.BadRequest(new { error = e.Message });
            }
        });

        // 关键词词频 TopN : ?top=30
        group.MapGet("/keywords", (AnalysisQueryService query, DateTime? start, DateTime? end, int? top) =>
        {
            var (startAt, endAt) = DefaultWindow(start, end);
            try
            {
                return Results.Ok(query.Keywords(startAt, endAt, top ?? 30));
            }
            catch (ArgumentException e)
            {
                return Results.BadRequest(new { error = e.Message });
            }
        });
    }

    /// <summary>默认时间窗 : 最近 7 天 ( end 为明天 0 点 , 左闭右开 ) ; 统一去掉 Kind 防止 Npgsql 当 timestamptz</summary>
    private static (DateTime Start, DateTime End) DefaultWindow(DateTime? start, DateTime? end)
    {
        var endAt = DateTime.SpecifyKind((end ?? DateTime.Now).Date.AddDays(1), DateTimeKind.Unspecified);
        var startAt = DateTime.SpecifyKind(start ?? endAt.AddDays(-7), DateTimeKind.Unspecified);
        return (startAt, endAt);
    }
}
