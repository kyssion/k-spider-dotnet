using k_spider_dotnet.script;

namespace k_spider_dotnet.tool.http;

public static class HtmlGetImgDownLoad
{
    public static async Task<ImgInfo> DownloadImgAsByte(string url)
    {
        var ans = new ImgInfo();
        var httpResponse = await new HttpClient().GetAsync(url);
        var dataByte = await httpResponse.Content.ReadAsByteArrayAsync();
        ans.ImgName =  HttpUrlTool.GetUrlLastPath(url);
        ans.Data = dataByte;
        ans.ResourceUrl = url;
        return ans;
    }
    

    public static void AddImgInfoIntoDirectory(ImgInfo imgInfo, string baseDirectory)
    {
        var directory = Environment.OSVersion.Platform switch
        {
            PlatformID.Unix => DataResource.MaxOsImagePath + baseDirectory,
            PlatformID.MacOSX => DataResource.MaxOsImagePath + baseDirectory,
            _ => throw new Exception($"os not support  : {Environment.OSVersion.Platform}")
        };
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
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
        if (ansContextType == HttpHeaderTool.Jpg.TypeString) return ".jpg";

        if (ansContextType == HttpHeaderTool.Png.TypeString) return ".jpg";

        if (ansContextType == HttpHeaderTool.Webp.TypeString) return ".webp";

        if (ansContextType == HttpHeaderTool.Gif.TypeString) return ".gif";

        if (ansContextType == HttpHeaderTool.Jp2.TypeString) return ".jp2";

        throw new Exception("not find content type img");
    }

    private static string GetImgNameFromUrl(string url)
    {
        var fileName = HttpUrlTool.GetUrlLastPath(url);
        return fileName.Split('.', 2)[0];
    }

    public struct ImgInfo
    {
        public string ResourceUrl { get; set; }
        public string ImgName { get; set; }
        public byte[] Data { get; set; }
    }
}