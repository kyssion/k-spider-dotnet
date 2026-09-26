namespace KSpider.Tool.Http;

/// <summary>
///     常见图片 Content-Type 的具名常量 ( 按响应头定图片文件后缀用 )
/// </summary>
public static class HttpHeaderTools
{
    public static readonly ContentTypeInfo Jpg = new() { TypeString = "image/jpeg" };
    public static readonly ContentTypeInfo Jp2 = new() { TypeString = "image/jp2" };
    public static readonly ContentTypeInfo Webp = new() { TypeString = "image/webp" };
    public static readonly ContentTypeInfo Png = new() { TypeString = "image/png" };
    public static readonly ContentTypeInfo Gif = new() { TypeString = "image/gif" };

    /// <summary>
    ///     Content-Type 字符串的具名包装 , 让常量有独立类型可读
    /// </summary>
    public struct ContentTypeInfo
    {
        public string TypeString { get; set; }
    }
}