using HtmlAgilityPack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace k_spider_dotnet_test;

class Mmmm
{
    public static Mmmm Iiii = new Mmmm(){ iiii = "123",Item = null};
    public required Mmmm? Item { get; init; }
    public string iiii
    {
        get;
        set;
    }
    
    public void show()
    {
                
    }
}

[TestClass]
public class GetImgInfo
{

    [TestMethod]

    public void TestMain()
    {
        var iiiis = new Mmmm
        {
            iiii = "123",
            Item = null
        };
        Console.WriteLine(iiiis.Item?.iiii??"is null");
    } 
    public static string BashPath =
        "C:\\Users\\14099\\Documents\\project\\dotnet\\k-spider-dotnet\\k-spider-dotnet-test";
    [TestMethod]
    public void GetTextInfo()
    {
        var textFile = "text.html";
        var htmlStr = System.IO.File.ReadAllText(BashPath+"\\"+textFile);
        HtmlDocument htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(htmlStr);

        var result = new List<Dictionary<string, string>>();

        var items = htmlDoc.DocumentNode.SelectNodes("//div[@class='paragraph-editor-container-item' or @class='paragraph paragraph--portrait paragraph--selected']");
        foreach (var item in items)
        {
            var noNode = item.SelectSingleNode(".//div[@class='paragraph-editor-container-item-order']");
            var valueNode = item.SelectSingleNode(".//input[@c" +
                                                  "lass='arco-input arco-input-size-medium']");

            if (noNode != null && valueNode != null)
            {
                var no = noNode.InnerText.Trim();
                var value = valueNode.GetAttributeValue("value", "").Trim();

                result.Add(new Dictionary<string, string>
                {
                    { "no", no },
                    { "value", value }
                });
            }
        }

        string jsonOutput = JsonConvert.SerializeObject(result, Formatting.Indented);
        Console.WriteLine(jsonOutput);
        // 创建保存图片的目录
        string saveDirectory = "DownloadedImages";
        saveDirectory =BashPath+"\\"+saveDirectory;
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }
        string savePath = Path.Combine(saveDirectory, "文案信息.json");
        File.WriteAllText(savePath, jsonOutput);
        Console.WriteLine("Download completed!");
    }

    [TestMethod]
    public void GetImgeInfo()
    {
        var textFile = "img.html";
        var htmlStr = System.IO.File.ReadAllText(BashPath+"\\"+textFile);
        HtmlDocument htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(htmlStr);

        var result = new List<Dictionary<string, string>>();

        var items = htmlDoc.DocumentNode.SelectNodes("//div[@class='paragraph paragraph--portrait']");
       
        foreach (var item in items)
        {
            var noNode = item.SelectSingleNode(".//div[@class='paragraph-header']");
            var imgNode = item.SelectSingleNode(".//div[@class='paragraph-img-wrap']/img");

            if (noNode != null && imgNode != null)
            {
                var no = noNode.InnerText.Trim();
                var imgSrc = imgNode.GetAttributeValue("src", "").Trim();

                result.Add(new Dictionary<string, string>
                {
                    { "no", no },
                    { "value", imgSrc }
                });
            }
        }

        string jsonOutput = JsonConvert.SerializeObject(result, Formatting.Indented);
        // 解析 JSON 数据
        var imageData = JsonConvert.DeserializeObject<List<ImageInfo>>(jsonOutput);

        // 创建保存图片的目录
        string saveDirectory = "DownloadedImages";
        saveDirectory =BashPath+"\\"+saveDirectory;
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }

        // 使用 HttpClient 下载图片
        using (HttpClient httpClient = new HttpClient())
        {
            foreach (var image in imageData)
            {
                string imageUrl = image.Value;
                string fileName = $"{image.No}.jpg"; // 使用编号作为文件名
                string savePath = Path.Combine(saveDirectory, fileName);
                
                try
                {
                    Console.WriteLine($"Downloading: {imageUrl}");
                    byte[] imageBytes = httpClient.GetByteArrayAsync(imageUrl).Result;
                    File.WriteAllBytesAsync(savePath, imageBytes).Wait();
                    Console.WriteLine($"Saved: {savePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to download {imageUrl}: {ex.Message}");
                }
            }
        }
        Console.WriteLine("Download completed!");
    }
}

public class ImageInfo
{
    public string No { get; set; }
    public string Value { get; set; }
}