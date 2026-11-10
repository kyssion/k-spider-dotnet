using KSpider.Admin.Api.Contracts;
using KSpider.Admin.Common.Logging;
using KSpider.Admin.Repository.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace KSpider.Admin.Api;

/// <summary>
///     API 统一异常边界：把数据库异常与未处理异常转换为稳定的 JSON 错误。
/// </summary>
/// <remarks>
///     客户端主动断开时不再写响应；响应已开始时也不尝试追加错误体。日志保留完整异常，
///     响应体只暴露错误码，避免泄漏数据库细节。
/// </remarks>
/// <param name="next">下一个中间件。</param>
/// <param name="logger">结构化日志。</param>
public sealed class ApiExceptionMiddleware(
    RequestDelegate next,
    ILogger<ApiExceptionMiddleware> logger)
{
    /// <summary>
    ///     执行中间件。
    /// </summary>
    /// <param name="context">当前请求上下文。</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // 客户端已断开，不再写响应。
        }
        catch (KDbException exception)
        {
            AdminLog.ApiDatabaseException(
                logger,
                exception,
                context.Request.Method,
                context.Request.Path.Value ?? string.Empty);
            await WriteErrorAsync(context, "database_error");
        }
        catch (Exception exception)
        {
            AdminLog.ApiUnhandledException(
                logger,
                exception,
                context.Request.Method,
                context.Request.Path.Value ?? string.Empty);
            await WriteErrorAsync(context, "internal_error");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, string error)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new ApiError(error));
    }
}
