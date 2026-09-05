namespace k_spider_dotnet.tool.http;

public static class HttpHeaderTools
{
    public static readonly ContentTypeInfo Jpg = new() { TypeString = "image/jpeg" };
    public static readonly ContentTypeInfo Jp2 = new() { TypeString = "image/jp2" };
    public static readonly ContentTypeInfo Webp = new() { TypeString = "image/webp" };
    public static readonly ContentTypeInfo Png = new() { TypeString = "image/png" };
    public static readonly ContentTypeInfo Gif = new() { TypeString = "image/gif" };

    public struct ContentTypeInfo
    {
        public string TypeString { get; set; }
    }
}