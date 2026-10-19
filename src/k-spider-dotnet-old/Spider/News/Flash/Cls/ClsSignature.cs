using System.Security.Cryptography;
using System.Text;

namespace KSpider.Spider.News.Flash.Cls;

/// <summary>
///     财联社接口签名 : sign = MD5( SHA1( 参数按 key 升序拼接的 query ) )
///     算法取自前端 bundle ( 取值不做 URL 编码 , 空值参数丢弃 ) ;
///     单测里锁了一组实测向量 , 改动这里必须同步那条用例。
///     这是**财联社站级共享** ( 电报与文章两条管线同一套签名 ) : 物理放在先出现的电报管线目录 ,
///     文章管线 ( Spider/News/Web/Cls ) 显式 using 借用——同站跨管线共享的预期模式 , 只有这一处
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

    /// <summary>
    ///     对拼好的 query 串计算签名 : 先 SHA1 再 MD5 , 输出小写十六进制
    /// </summary>
    public static string Sign(string queryString)
    {
        var sha1Hex = Convert.ToHexStringLower(SHA1.HashData(Encoding.UTF8.GetBytes(queryString)));
        return Convert.ToHexStringLower(MD5.HashData(Encoding.UTF8.GetBytes(sha1Hex)));
    }
}
