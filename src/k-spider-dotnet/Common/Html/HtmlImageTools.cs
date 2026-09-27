using KSpider.Common.Http;

namespace KSpider.Common.Html;

/// <summary>
///     图片下载与落盘工具 ( 开发期产物 , 下载 / 落盘方法当前无调用方 ) ;
///     生产链路只复用 ImgInfo 结构在解析阶段承载图片信息 ( 见 DfContentSpider )
/// </summary>
public static class HtmlImageTools
{
    // 开发期写死的本机输出路径 , 下载 / 落盘流程未接入生产
    public const string MaxOsImagePath = "/Users/bytedance/RiderProjects/k-spider-dotnet/newsImg/";

    /// <summary>
    ///     下载图片字节并回填到 ImgInfo.Data
    /// </summary>
    public static async Task<ImgInfo> DownloadImgAsByteInto(ImgInfo imgInfo)
    {
        var httpResponse = await HttpClientTools.GetHttpClient().GetAsync(imgInfo.ResourceUrl);
        var dataByte = await httpResponse.Content.ReadAsByteArrayAsync();
        imgInfo.Data = dataByte;
        return imgInfo;
    }


    /// <summary>
    ///     落盘到本地目录 : 仅支持 mac ( 其它平台抛异常 ) , 目录不存在时自动创建
    /// </summary>
    public static void AddImgInfoIntoDirectory(ImgInfo imgInfo, string baseDirectory)
    {
        var directory = Environment.OSVersion.Platform switch
        {
            PlatformID.Unix => MaxOsImagePath + baseDirectory,
            PlatformID.MacOSX => MaxOsImagePath + baseDirectory,
            _ => throw new Exception($"os not support  : {Environment.OSVersion.Platform}")
        };
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        var file = new FileStream(directory + "/" + imgInfo.ImgName, FileMode.Create);
        var w = new BinaryWriter(file);
        try
        {
            w.Write(imgInfo.Data);
        }
        finally
        {
            file.Close();
            w.Close();
        }
    }

    /// <summary>
    ///     按 Content-Type 定文件后缀 ( 注意 : png 分支返回的同样是 .jpg , 既有行为 )
    /// </summary>
    private static string GetImageSuffixByContentType(string ansContextType)
    {
        if (ansContextType == HttpHeaderTools.Jpg.TypeString) return ".jpg";

        if (ansContextType == HttpHeaderTools.Png.TypeString) return ".jpg";

        if (ansContextType == HttpHeaderTools.Webp.TypeString) return ".webp";

        if (ansContextType == HttpHeaderTools.Gif.TypeString) return ".gif";

        if (ansContextType == HttpHeaderTools.Jp2.TypeString) return ".jp2";

        throw new Exception("not find content type img");
    }

    private static string GetImgNameFromUrl(string url)
    {
        var fileName = HttpUrlTools.GetUrlLastPath(url);
        return fileName.Split('.', 2)[0];
    }

    /// <summary>
    ///     图片信息载体 : 解析阶段从正文提取的资源 URL 与归属新闻 , 下载流程再回填字节
    /// </summary>
    public struct ImgInfo
    {
        public string ResourceUrl { get; set; }
        public string ImgName { get; set; }
        public byte[] Data { get; set; }

        public string NewsUrl { get; set; }
    }
}
