using KSpider.Admin.Api.Contracts;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KSpider.Admin.Auth;

/// <summary>
///     认证端点：登录、登出、修改密码与当前用户。
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    ///     映射 /api/auth 端点组。
    /// </summary>
    /// <param name="endpoints">端点路由构建器。</param>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth");

        group.MapPost("/login", LoginAsync).AllowAnonymous();
        group.MapPost("/logout", LogoutAsync);
        group.MapPost("/change-password", ChangePasswordAsync);
        group.MapGet("/me", MeAsync);

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        HttpContext context,
        LoginRequest request,
        AuthService auth,
        CancellationToken cancellationToken)
    {
        var outcome = await auth.LoginAsync(context, request, cancellationToken);
        return outcome switch
        {
            LoginOutcome.Success => Results.NoContent(),
            LoginOutcome.OriginRejected => Results.Json(
                new ApiError("forbidden_origin"),
                statusCode: StatusCodes.Status403Forbidden),
            _ => Results.Json(
                new ApiError("unauthorized", "用户名或密码错误"),
                statusCode: StatusCodes.Status401Unauthorized),
        };
    }

    private static async Task<IResult> LogoutAsync(HttpContext context, AuthService auth)
    {
        await auth.SignOutAsync(context);
        return Results.NoContent();
    }

    private static async Task<IResult> ChangePasswordAsync(
        HttpContext context,
        ChangePasswordRequest request,
        AuthService auth,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUser.GetUserId(context.User);
        if (userId == 0)
        {
            return Results.Json(new ApiError("unauthorized"), statusCode: StatusCodes.Status401Unauthorized);
        }

        var outcome = await auth.ChangePasswordAsync(userId, request, cancellationToken);
        return outcome switch
        {
            ChangePasswordOutcome.Success => Results.NoContent(),
            ChangePasswordOutcome.NotFound => Results.NotFound(new ApiError("not_found")),
            ChangePasswordOutcome.InvalidCurrentPassword => Results.BadRequest(
                new ApiError("invalid_current_password")),
            ChangePasswordOutcome.InvalidNewPassword => Results.BadRequest(new ApiError("invalid_new_password")),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    private static async Task<IResult> MeAsync(
        HttpContext context,
        UserService users,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUser.GetUserId(context.User);
        if (userId == 0)
        {
            return Results.Json(new ApiError("unauthorized"), statusCode: StatusCodes.Status401Unauthorized);
        }

        var user = await users.GetAsync(userId, cancellationToken);
        if (user is null)
        {
            return Results.Json(new ApiError("unauthorized"), statusCode: StatusCodes.Status401Unauthorized);
        }

        var tokens = antiforgery.GetAndStoreTokens(context);
        return Results.Ok(new MeView(user.Id, user.Username, user.DisplayName, tokens.RequestToken ?? string.Empty));
    }
}
