using System.Web;

namespace KSpider.Tool.Http;

public class HttpUrlTools
{
    public static string[] GetUrlParamInfo(string url, string paramKey)
    {
        var uri = new Uri(url);
        // 获取查询字符串
        var queryString = uri.Query;
        // 解析查询字符串
        var queryCollection = HttpUtility.ParseQueryString(queryString);
        foreach (var key in queryCollection.AllKeys)
            if (key == paramKey)
                return queryCollection.GetValues(key) ?? [];
        return [];
    }

    public static string GetUrlLastPath(string url)
    {
        // URL 拆分出 ? 之前的数据
        url = url.Split("?", 2)[0];
        // 获取最后一个路径
        var lastSlashIndex = url.LastIndexOf('/');
        if (lastSlashIndex == -1) return string.Empty;
        var lastPath = url[(lastSlashIndex + 1)..];
        return lastPath;
    }
}