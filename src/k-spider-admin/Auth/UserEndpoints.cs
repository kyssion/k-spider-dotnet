using KSpider.Admin.Api.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KSpider.Admin.Auth;

/// <summary>
///     账号管理端点。
/// </summary>
public static class UserEndpoints
{
    /// <summary>
    ///     映射 /api/users 端点组。
    /// </summary>
    /// <param name="endpoints">端点路由构建器。</param>
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users");

        group.MapGet("", ListAsync);
        group.MapPost("", CreateAsync);
        group.MapPatch("/{id:long}", PatchAsync);
        group.MapPost("/{id:long}/reset-password", ResetPasswordAsync);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(UserService users, CancellationToken cancellationToken)
    {
        return Results.Ok(await users.ListAsync(cancellationToken));
    }

    private static async Task<IResult> CreateAsync(
        HttpContext context,
        UserCreateRequest request,
        UserService users,
        CancellationToken cancellationToken)
    {
        var operatorId = CurrentUser.GetUserId(context.User);
        if (operatorId == 0)
        {
            return Results.Json(new ApiError("unauthorized"), statusCode: StatusCodes.Status401Unauthorized);
        }

        var outcome = await users.CreateAsync(request, operatorId, cancellationToken);
        return outcome == UserMutationOutcome.Success
            ? Results.StatusCode(StatusCodes.Status201Created)
            : MapFailure(outcome);
    }

    private static async Task<IResult> PatchAsync(
        HttpContext context,
        long id,
        UserPatchRequest request,
        UserService users,
        CancellationToken cancellationToken)
    {
        var operatorId = CurrentUser.GetUserId(context.User);
        if (operatorId == 0)
        {
            return Results.Json(new ApiError("unauthorized"), statusCode: StatusCodes.Status401Unauthorized);
        }

        var outcome = await users.PatchAsync(id, request, operatorId, cancellationToken);
        return outcome == UserMutationOutcome.Success ? Results.NoContent() : MapFailure(outcome);
    }

    private static async Task<IResult> ResetPasswordAsync(
        HttpContext context,
        long id,
        ResetPasswordRequest request,
        UserService users,
        CancellationToken cancellationToken)
    {
        var operatorId = CurrentUser.GetUserId(context.User);
        if (operatorId == 0)
        {
            return Results.Json(new ApiError("unauthorized"), statusCode: StatusCodes.Status401Unauthorized);
        }

        var outcome = await users.ResetPasswordAsync(id, request.NewPassword, operatorId, cancellationToken);
        return outcome == UserMutationOutcome.Success ? Results.NoContent() : MapFailure(outcome);
    }

    private static IResult MapFailure(UserMutationOutcome outcome)
    {
        return outcome switch
        {
            UserMutationOutcome.ValidationError => Results.BadRequest(new ApiError("validation")),
            UserMutationOutcome.NotFound => Results.NotFound(new ApiError("not_found")),
            UserMutationOutcome.Conflict => Results.Conflict(new ApiError("conflict")),
            UserMutationOutcome.Forbidden => Results.Json(
                new ApiError("forbidden"),
                statusCode: StatusCodes.Status403Forbidden),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
