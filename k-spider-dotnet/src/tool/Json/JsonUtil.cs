using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace k_spider_dotnet.tool.Json;

public class JsonUtil
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };
    public static string GetJson(object item)
    {
        return JsonSerializer.Serialize(item, JsonSerializerOptions);
    }
}