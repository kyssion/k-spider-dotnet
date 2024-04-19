using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;

namespace k_spider_dotnet.tool.html;

public static class HtmlGetImgDownLoad
{
    public struct ImgInfo
    {
        public string ImgName { get; set; }
        public string ContextType { get; set; }
        public byte[] Data { get; set; }
    }
    public static async Task<ImgInfo> DownloadImgAsByte(string url, string imgName)
    {
        var ans = new ImgInfo();
        var httpResponse = await new HttpClient().GetAsync(url);
        foreach (var contextType in httpResponse.Content.Headers.GetValues("Content-Type"))
        {
            ans.ContextType = contextType;
        }
        var dataByte = await httpResponse.Content.ReadAsByteArrayAsync();
        ans.ImgName = imgName + GetImageSuffixByContentType(ans.ContextType);
        ans.Data = dataByte;
        return ans;
    }

    public static async Task<string> DownloadImgToFilePath(string filePath ,string url, string imgName)
    {
        Console.WriteLine(System.Environment.CurrentDirectory);
        var imgInfo = await DownloadImgAsByte(url, imgName);
        var file = new FileStream(imgInfo.ImgName, FileMode.Create);
        var w = new BinaryWriter(file);
        try
        {
            w.Write(imgInfo.Data);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            file.Close();
            w.Close();
        }

        return imgInfo.ImgName;
    }

    private static string GetImageSuffixByContentType(string ansContextType)
    {
        if (ansContextType == HttpHeaderTool.Jpg.TypeString)
        {
            return ".jpg";
        }
        if (ansContextType == HttpHeaderTool.Png.TypeString)
        {
            return ".jpg";
        }
        if (ansContextType == HttpHeaderTool.Webp.TypeString)
        {
            return ".webp";
        }
        if (ansContextType == HttpHeaderTool.Gif.TypeString)
        {
            return ".gif";
        }
        if (ansContextType == HttpHeaderTool.Jp2.TypeString)
        {
            return ".jp2";
        }

        throw new Exception("not find content type img");
    }
}