using System.Text.Encodings.Web;
using System.Text.Json;

namespace KSpider.Common;

/// <summary>
///     JSON 序列化统一入口 : 请求响应解析、落库字段、飞书消息体都走这里
/// </summary>
public static class JsonTools
{
    // UnsafeRelaxedJsonEscaping : 中文等非 ASCII 字符不转义成 \uXXXX , 落库与日志保持可读 ;
    // IncludeFields : 支持 struct 的公有字段序列化 ( 飞书模板体等 )
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        IncludeFields = true
    };

    public static string GetJson(object item)
    {
        return JsonSerializer.Serialize(item, JsonSerializerOptions);
    }
}
