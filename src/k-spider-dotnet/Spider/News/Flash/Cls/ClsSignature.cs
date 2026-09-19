using System.Security.Cryptography;
using System.Text;

namespace KSpider.Spider.News.Flash.Cls;

/// <summary>
///     财联社接口签名 : sign = MD5( SHA1( 参数按 key 升序拼接的 query ) )
///     算法取自前端 bundle ( 取值不做 URL 编码 , 空值参数丢弃 ) ;
///     单测里锁了一组实测向量 , 改动这里必须同步那条用例
/// </summary>
public static class ClsSignature
{
    /// <summary>
    ///     按 key 升序拼成 k=v&amp;k=v , 丢弃空值 ( 与前端 Object.keys().sort() 后 filter 的行为一致 )
    /// </summary>
    public static string BuildQueryString(IReadOnlyDictionary<string, string> parameters)
    {
        return string.Join("&", parameters
            .Where(item => !string.IsNullOrEmpty(item.Value))
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .Select(item => $"{item.Key}={item.Value}"));
    }

    public static string Sign(string queryString)
    {
        var sha1Hex = Convert.ToHexStringLower(SHA1.HashData(Encoding.UTF8.GetBytes(queryString)));
        return Convert.ToHexStringLower(MD5.HashData(Encoding.UTF8.GetBytes(sha1Hex)));
    }
}
