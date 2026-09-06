using System.Text.Encodings.Web;
using System.Text.Json;

namespace k_spider_dotnet.json;

public static class JsonUtil
{
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