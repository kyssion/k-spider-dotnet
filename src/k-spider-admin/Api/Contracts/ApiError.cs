using System.Text.Json.Serialization;

namespace KSpider.Admin.Api.Contracts;

/// <summary>
///     统一 API 错误响应。
/// </summary>
/// <param name="Error">稳定的机器可读错误码，如 unauthorized / not_found / conflict。</param>
/// <param name="Message">可选的人类可读说明；为空时不序列化。</param>
public sealed record ApiError(
    string Error,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Message = null);
