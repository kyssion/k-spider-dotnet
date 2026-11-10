using System.Text.Json;
using System.Text.Json.Serialization;

namespace KSpider.Admin.Common.Json;

/// <summary>
///     admin 内部 JSONB 契约（task_detail / extra / channel.schedule / device）统一序列化选项。
///     键名沿用 snake_case，与 docs/10-architecture 的约定键保持一致。
/// </summary>
public static class AdminJson
{
    /// <summary>
    ///     admin 共享的 JSONB 序列化选项。
    /// </summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    ///     按 admin 约定序列化 JSONB 内容。
    /// </summary>
    /// <param name="value">待序列化对象。</param>
    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, Options);
    }

    /// <summary>
    ///     按 admin 约定反序列化 JSONB 内容。
    /// </summary>
    /// <param name="json">JSON 文本。</param>
    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, Options);
    }

    /// <summary>
    ///     序列化为 JsonElement，便于在 JSONB 合并前检查结构。
    /// </summary>
    /// <param name="value">待序列化对象。</param>
    public static JsonElement SerializeToElement<T>(T value)
    {
        return JsonSerializer.SerializeToElement(value, Options);
    }
}
