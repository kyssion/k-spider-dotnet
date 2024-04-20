using System.Web;

namespace k_spider_dotnet.tool.http;

public class HttpUrlTool
{
    public static string[] GetUrlParamInfo(string url , string paramKey)
    {
        var uri = new Uri(url);
        // 获取查询字符串
        string queryString = uri.Query;
        // 解析查询字符串
        var queryCollection = HttpUtility.ParseQueryString(queryString);
        foreach (var key in queryCollection.AllKeys)
        {
            if (key == paramKey)
            {
                return queryCollection.GetValues(key)?? [];
            }
        }
        return [];
    }
}