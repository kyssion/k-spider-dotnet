using KSpider.Web.Query;
using Microsoft.AspNetCore.Builder;

namespace KSpider.Web.Api;

/// <summary>
///     实时快讯端点 : /api/flash/*
/// </summary>
public static class FlashEndpoints
{
    public static void MapFlashApis(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/flash").WithTags("实时快讯");

        // 分页筛选 : ?page=1&pageSize=20&source=&category=&level=&start=&end=&keyword=
        group.MapGet("/list",
            (FlashQueryService query, int page, int pageSize, int? source, int? category, int? level,
                DateTime? start, DateTime? end, string? keyword) =>
                query.List(page, pageSize, source, category, level, start, end, keyword));
    }
}
