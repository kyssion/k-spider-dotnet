using KSpider.Admin.Api.Contracts;
using KSpider.Admin.Api.Queries;
using KSpider.Admin.Repository.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KSpider.Admin.Api.Endpoints;

/// <summary>
///     原始数据只读端点。
/// </summary>
public static class RawDataEndpoints
{
    private static readonly HashSet<string> ExecStatuses =
        new(["pending", "running", "failed", "success"], StringComparer.Ordinal);

    private static readonly HashSet<string> DataStatuses =
        new(["normal", "filtered", "dead", "paywalled"], StringComparer.Ordinal);

    /// <summary>
    ///     映射 /api/raw-data 端点组。
    /// </summary>
    /// <param name="endpoints">端点路由构建器。</param>
    public static IEndpointRouteBuilder MapRawDataEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/raw-data");

        group.MapGet("/sources", ListSourcesAsync);
        group.MapGet("/sources/{id:long}", GetSourceAsync);
        group.MapGet("/sources/{id:long}/details", ListDetailsAsync);
        group.MapGet("/details/{id:long}", GetDetailAsync);

        return endpoints;
    }

    private static async Task<IResult> ListSourcesAsync(
        RawDataQueryService query,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20,
        long? channelId = null,
        DateTime? from = null,
        DateTime? to = null,
        string? execStatus = null,
        string? dataStatus = null)
    {
        if (!IsValidStatus(execStatus, ExecStatuses) ||
            !IsValidStatus(dataStatus, DataStatuses) ||
            (from is not null && to is not null && from >= to))
        {
            return Results.BadRequest(new ApiError("validation"));
        }

        return Results.Ok(await query.QuerySourcesAsync(
            new RawDataSourceQuery(page, pageSize, channelId, from, to, execStatus, dataStatus),
            cancellationToken));
    }

    private static async Task<IResult> GetSourceAsync(
        long id,
        RawDataQueryService query,
        CancellationToken cancellationToken)
    {
        var source = await query.GetSourceAsync(id, cancellationToken);
        return source is null ? Results.NotFound(new ApiError("not_found")) : Results.Ok(source);
    }

    private static async Task<IResult> ListDetailsAsync(
        long id,
        RawDataQueryService query,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20)
    {
        var details = await query.QueryDetailsAsync(id, page, pageSize, cancellationToken);
        return details is null ? Results.NotFound(new ApiError("not_found")) : Results.Ok(details);
    }

    private static async Task<IResult> GetDetailAsync(
        long id,
        RawDataQueryService query,
        CancellationToken cancellationToken)
    {
        var detail = await query.GetDetailAsync(id, cancellationToken);
        return detail is null ? Results.NotFound(new ApiError("not_found")) : Results.Ok(detail);
    }

    private static bool IsValidStatus(string? value, HashSet<string> allowed)
    {
        return string.IsNullOrWhiteSpace(value) || allowed.Contains(value);
    }
}
