using KSpider.Admin.Api.Contracts;
using KSpider.Admin.Api.Queries;
using KSpider.Admin.Auth;
using KSpider.Admin.Repository.Models;
using KSpider.Admin.Scheduler;
using KSpider.Admin.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KSpider.Admin.Api.Endpoints;

/// <summary>
///     控制台管理端点：渠道、任务、节点与分类查询。
/// </summary>
public static class ManagementEndpoints
{
    /// <summary>
    ///     映射 /api 管理端点组。
    /// </summary>
    /// <param name="endpoints">端点路由构建器。</param>
    public static IEndpointRouteBuilder MapManagementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api");

        group.MapGet("/overview", GetOverviewAsync);
        group.MapGet("/channels", ListChannelsAsync);
        group.MapGet("/channels/{id:long}", GetChannelAsync);
        group.MapPost("/channels/{id:long}/pause", PauseChannelAsync);
        group.MapPost("/channels/{id:long}/resume", ResumeChannelAsync);
        group.MapGet("/channel-categories", ListCategoriesAsync);

        group.MapGet("/tasks", ListTasksAsync);
        group.MapGet("/tasks/{id:long}", GetTaskAsync);
        group.MapPost("/tasks/{id:long}/cancel", CancelTaskAsync);

        group.MapGet("/nodes", ListNodesAsync);
        group.MapGet("/nodes/{nodeId:guid}", GetNodeAsync);

        return endpoints;
    }

    private static async Task<IResult> GetOverviewAsync(
        OverviewQueryService query,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await query.GetAsync(cancellationToken));
    }

    private static async Task<IResult> ListChannelsAsync(
        ChannelQueryService query,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20,
        long? categoryId = null,
        string? status = null)
    {
        return Results.Ok(await query.QueryAsync(
            new ChannelQuery(page, pageSize, categoryId, status),
            cancellationToken));
    }

    private static async Task<IResult> GetChannelAsync(
        long id,
        ChannelQueryService query,
        CancellationToken cancellationToken)
    {
        var channel = await query.GetAsync(id, cancellationToken);
        return channel is null ? Results.NotFound(new ApiError("not_found")) : Results.Ok(channel);
    }

    private static async Task<IResult> PauseChannelAsync(
        HttpContext context,
        long id,
        ChannelCommandService commands,
        CancellationToken cancellationToken)
    {
        var operatorId = CurrentUser.GetUserId(context.User);
        if (operatorId == 0)
        {
            return Results.Json(new ApiError("unauthorized"), statusCode: StatusCodes.Status401Unauthorized);
        }

        return MapChannelCommand(await commands.PauseAsync(id, operatorId, cancellationToken));
    }

    private static async Task<IResult> ResumeChannelAsync(
        HttpContext context,
        long id,
        ChannelCommandService commands,
        CancellationToken cancellationToken)
    {
        var operatorId = CurrentUser.GetUserId(context.User);
        if (operatorId == 0)
        {
            return Results.Json(new ApiError("unauthorized"), statusCode: StatusCodes.Status401Unauthorized);
        }

        return MapChannelCommand(await commands.ResumeAsync(id, operatorId, cancellationToken));
    }

    private static async Task<IResult> ListCategoriesAsync(
        ChannelQueryService query,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await query.ListCategoriesAsync(cancellationToken));
    }

    private static async Task<IResult> ListTasksAsync(
        TaskQueryService query,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20,
        string? status = null,
        Guid? nodeId = null)
    {
        return Results.Ok(await query.QueryAsync(
            new TaskQuery(page, pageSize, status, nodeId),
            cancellationToken));
    }

    private static async Task<IResult> GetTaskAsync(
        long id,
        TaskQueryService query,
        CancellationToken cancellationToken)
    {
        var task = await query.GetAsync(id, cancellationToken);
        return task is null ? Results.NotFound(new ApiError("not_found")) : Results.Ok(task);
    }

    private static async Task<IResult> CancelTaskAsync(
        long id,
        IAdminTaskService adminTasks,
        CancellationToken cancellationToken,
        string? reason = null)
    {
        var result = await adminTasks.TryCancelAsync(
            id,
            string.IsNullOrWhiteSpace(reason) ? "人工取消" : reason,
            cancellationToken);

        return result.Outcome switch
        {
            TaskTransitionOutcome.Applied => Results.NoContent(),
            TaskTransitionOutcome.NotFound => Results.NotFound(new ApiError("not_found")),
            TaskTransitionOutcome.Conflict or TaskTransitionOutcome.InvalidTransition or
                TaskTransitionOutcome.FailLimitReached => Results.Conflict(new ApiError("conflict")),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    private static async Task<IResult> ListNodesAsync(
        NodeQueryService query,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await query.ListAsync(cancellationToken));
    }

    private static async Task<IResult> GetNodeAsync(
        Guid nodeId,
        NodeQueryService query,
        CancellationToken cancellationToken)
    {
        var node = await query.GetAsync(nodeId, cancellationToken);
        return node is null ? Results.NotFound(new ApiError("not_found")) : Results.Ok(node);
    }

    private static IResult MapChannelCommand(ChannelCommandOutcome outcome)
    {
        return outcome switch
        {
            ChannelCommandOutcome.Success => Results.NoContent(),
            ChannelCommandOutcome.NotFound => Results.NotFound(new ApiError("not_found")),
            ChannelCommandOutcome.Conflict => Results.Conflict(new ApiError("conflict")),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
