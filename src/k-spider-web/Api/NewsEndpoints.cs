using KSpider.Web.Query;
using Microsoft.AspNetCore.Builder;

namespace KSpider.Web.Api;

/// <summary>
///     网页型新闻端点 : /api/news/*
/// </summary>
public static class NewsEndpoints
{
    public static void MapNewsApis(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/news").WithTags("网页新闻");

        // 分页筛选 : ?page=1&pageSize=20&source=&category=&status=&start=&end=&keyword=
        group.MapGet("/list",
            (NewsQueryService query, int page, int pageSize, int? source, int? category, int? status,
                DateTime? start, DateTime? end, string? keyword) =>
                query.List(page, pageSize, source, category, status, start, end, keyword));

        // 详情 : 列表 + 原始 JSON + 解析内容 + 图片
        group.MapGet("/detail", (NewsQueryService query, string? newsUrl) =>
        {
            if (string.IsNullOrWhiteSpace(newsUrl)) return Results.BadRequest(new { error = "newsUrl 必填" });
            var detail = query.Detail(newsUrl);
            return detail == null ? Results.NotFound(new { error = "新闻不存在" }) : Results.Ok(detail);
        });
    }
}
