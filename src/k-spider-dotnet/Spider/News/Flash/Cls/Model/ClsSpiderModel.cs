using System.Text.Json.Nodes;
using KSpider.Common.Json;
using KSpider.Model;
using KSpider.Spider.News.Flash;
using KSpider.Tool.Http;

namespace KSpider.Spider.News.Flash.Cls.Model;

/// <summary>
///     电报列表接口的单条数据 ( 列表即全文 ) , 直接映射为可入库的快讯记录
/// </summary>
public class ClsRollItem
{
    /// <summary>
    ///     电报约半数条目没有标题 , 用摘要兜底时的截断长度
    /// </summary>
    private const int BriefTitleMaxLength = 60;

    /// <summary>
    ///     接口 ctime 为 unix 秒 ( 北京时间 ) , 固定按东八区换算 , 不依赖宿主时区
    /// </summary>
    private static readonly TimeSpan ChinaOffset = TimeSpan.FromHours(8);

    public long Id { get; set; }

    public string Title { get; set; } = "";

    public string Brief { get; set; } = "";

    public string Content { get; set; } = "";

    /// <summary>
    ///     发布时间 , unix 秒
    /// </summary>
    public long Ctime { get; set; }

    /// <summary>
    ///     重要度 , 接口原始值 A / B / C
    /// </summary>
    public string Level { get; set; } = "C";

    public string CoverImage { get; set; } = "";

    public List<string> Images { get; set; } = [];

    public List<string> Subjects { get; set; } = [];

    /// <summary>
    ///     关联标的的原始数组 ( 元素含 StockID / name 等 , 见 ToFlashNewsModel 的提取 )
    /// </summary>
    public JsonArray? StockList { get; set; }

    public string NewsUrl => string.Format(ClsNewsResource.DetailUrlTemplate, Id);

    public DateTime NewsTime => DateTimeOffset.FromUnixTimeSeconds(Ctime).ToOffset(ChinaOffset).DateTime;

    private string DisplayTitle => string.IsNullOrEmpty(Title) ? Truncate(Brief, BriefTitleMaxLength) : Title;

    /// <summary>
    ///     重要度映射 : A→3 重大 / B→2 重要 / C→1 普通
    /// </summary>
    private short FlashLevel => Level switch
    {
        "A" => 3,
        "B" => 2,
        _ => 1
    };

    public static ClsRollItem FromJson(JsonNode node)
    {
        return new ClsRollItem
        {
            Id = ReadLong(node["id"]),
            Title = node["title"]?.ToString() ?? "",
            Brief = node["brief"]?.ToString() ?? "",
            Content = node["content"]?.ToString() ?? "",
            Ctime = ReadLong(node["ctime"]),
            Level = node["level"]?.ToString() ?? "C",
            CoverImage = node["img"]?.ToString() ?? "",
            Images = ReadImageUrls(node["images"]),
            Subjects = ReadSubjectNames(node["subjects"]),
            StockList = node["stock_list"] as JsonArray
        };
    }

    public SpiderFlashNewsModel ToFlashNewsModel(string itemJson)
    {
        var imageUrls = AllImageUrls();
        return new SpiderFlashNewsModel
        {
            FromMedia = (int)FromTypeOfNews.ClsMedia,
            Category = ClsNewsResource.TelegraphCategoryNumber,
            NewsUrl = NewsUrl,
            NewsTime = NewsTime,
            Title = DisplayTitle,
            Content = Content,
            Keyword = string.Join(",", Subjects),
            Level = FlashLevel,
            StockList = ReadStockListJson(),
            ImageUrls = imageUrls.Count > 0 ? JsonUtil.GetJson(imageUrls) : null,
            RawContent = itemJson
        };
    }

    /// <summary>
    ///     关联标的提取为统一形态 [{"stock_id":"sz300476","name":"胜宏科技"}] ; 无标的返回 null
    /// </summary>
    private string? ReadStockListJson()
    {
        if (StockList is not { Count: > 0 }) return null;
        var result = new List<Dictionary<string, string>>();
        foreach (var stock in StockList)
        {
            var stockId = stock?["StockID"]?.ToString();
            if (string.IsNullOrEmpty(stockId)) continue;
            result.Add(new Dictionary<string, string>
            {
                ["stock_id"] = stockId,
                ["name"] = stock?["name"]?.ToString() ?? ""
            });
        }

        // 用 JsonUtil 序列化 : JsonNode.ToJsonString 会把中文转义成 \uXXXX , 与已入库 JSON 的风格不一致
        return result.Count > 0 ? JsonUtil.GetJson(result) : null;
    }

    /// <summary>
    ///     正文图片 + 封面图 ( 封面不在正文里时补上 ) , 保持出现顺序
    /// </summary>
    private List<string> AllImageUrls()
    {
        var imageUrls = new List<string>(Images);
        if (!string.IsNullOrEmpty(CoverImage) && !imageUrls.Contains(CoverImage)) imageUrls.Add(CoverImage);
        return imageUrls;
    }

    // 接口数值字段可能是数字或数字字符串 , 统一兜底解析
    private static long ReadLong(JsonNode? node)
    {
        return long.TryParse(node?.ToString(), out var value) ? value : 0;
    }

    private static List<string> ReadImageUrls(JsonNode? node)
    {
        if (node is not JsonArray imageArray) return [];
        return imageArray.Select(image => image?.ToString() ?? "").Where(url => url != "").ToList();
    }

    private static List<string> ReadSubjectNames(JsonNode? node)
    {
        if (node is not JsonArray subjectArray) return [];
        return subjectArray.Select(subject => subject?["subject_name"]?.ToString() ?? "")
            .Where(name => name != "").ToList();
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
