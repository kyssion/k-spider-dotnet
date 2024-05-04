namespace k_spider_dotnet.tool.http;

public static class HtmlImageTools
{
    public const string MaxOsImagePath = "/Users/bytedance/RiderProjects/k-spider-dotnet/newsImg/";

    public static async Task<ImgInfo> DownloadImgAsByteInto(ImgInfo imgInfo)
    {
        var httpResponse = await new HttpClient().GetAsync(imgInfo.ResourceUrl);
        var dataByte = await httpResponse.Content.ReadAsByteArrayAsync();
        imgInfo.Data = dataByte;
        return imgInfo;
    }


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

    public struct ImgInfo
    {
        public string ResourceUrl { get; set; }
        public string ImgName { get; set; }
        public byte[] Data { get; set; }

        public string NewsUrl { get; set; }
    }
}