namespace k_spider_dotnet.tool.html;

public class HttpHeaderTool
{
    public struct ContentTypeInfo
    {
        public string TypeString { get; set; }
    }
     public static readonly ContentTypeInfo Jpg = new ContentTypeInfo(){TypeString = "image/jpeg"};
     public static readonly ContentTypeInfo Jp2 = new ContentTypeInfo(){TypeString = "image/jp2"};
     public static readonly ContentTypeInfo Webp = new ContentTypeInfo(){TypeString = "image/webp"};
     public static readonly ContentTypeInfo Png = new ContentTypeInfo(){TypeString = "image/png"};
     public static readonly ContentTypeInfo Gif = new ContentTypeInfo(){TypeString = "image/gif"};

}
