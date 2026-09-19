using System.Text.Json.Nodes;
using KSpider.Common.Json;
using KSpider.Model;
using KSpider.Spider.News.Flash;
using KSpider.Common.Time;

namespace KSpider.Spider.News.Flash.Jin10.Model;

/// <summary>
///     金十数据快讯的单条数据 ( 列表即全文 ) , 直接映射为可入库的快讯记录
/// </summary>
public class Jin10FlashItem
{
    /// <summary>
    ///     无标题快讯用正文兜底的截断长度
    /// </summary>
    private const int TitleMaxLength = 60;

    /// <summary>
    ///     id 是时间戳风格的字符串 ( 如 20260919105910403800 ) , 保持字符串形态
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    ///     发布时间 ( 北京时间字符串 , 无时区标记 )
    /// </summary>
    public string Time { get; set; } = "";

    /// <summary>
    ///     条目类型 : 0 普通快讯 , 2 图文 / 热榜类 ( 带 title 与 link )
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    ///     1 = 重要快讯
    /// </summary>
    public int Important { get; set; }

    public string Content { get; set; } = "";

    public string Title { get; set; } = "";

    /// <summary>
    ///     PLUS 专享条目的标题 ; 这类条目 content 为空且 lock=true ( 实测约占 20% )
    /// </summary>
    public string VipTitle { get; set; } = "";

    public string Pic { get; set; } = "";

    public List<string> Tags { get; set; } = [];

    public string NewsUrl => string.Format(Jin10NewsResource.DetailUrlTemplate, Id);

    public DateTime NewsTime => TimeTools.GetDateByTimeStrForFormat(Time, TimeTools.TimeFormatForStrikethrough);

    /// <summary>
    ///     PLUS 专享条目正文为空 ( 需付费账号 ) , 用 vip_title 兜底 , 至少保留标题信息 ;
    ///     是否属于专享条目可从原始 JSON 的 data.lock / data.exclusive_to 判断
    /// </summary>
    private string EffectiveContent => string.IsNullOrEmpty(Content) ? VipTitle : Content;

    /// <summary>
    ///     正文与标题 ( 含 vip_title 兜底 ) 全为空 : 条目没有任何公开信息 ( PLUS 锁定且无 vip_title ) , 不值得入库
    /// </summary>
    public bool HasNoPublicContent =>
        string.IsNullOrEmpty(Title) && string.IsNullOrEmpty(EffectiveContent);

    private string DisplayTitle => !string.IsNullOrEmpty(Title) ? Title : ExtractTitle(EffectiveContent);

    /// <summary>
    ///     重要度映射 : important=1 → 2 重要 , 其余 → 1 普通
    /// </summary>
    private short FlashLevel => (short)(Important == 1 ? 2 : 1);

    public static Jin10FlashItem FromJson(JsonNode node)
    {
        return new Jin10FlashItem
        {
            Id = node["id"]?.ToString() ?? "",
            Time = node["time"]?.ToString() ?? "",
            Type = int.TryParse(node["type"]?.ToString(), out var type) ? type : 0,
            Important = int.TryParse(node["important"]?.ToString(), out var important) ? important : 0,
            Content = node["data"]?["content"]?.ToString() ?? "",
            Title = node["data"]?["title"]?.ToString() ?? "",
            VipTitle = node["data"]?["vip_title"]?.ToString() ?? "",
            Pic = node["data"]?["pic"]?.ToString() ?? "",
            Tags = ReadStringArray(node["tags"])
        };
    }

    public SpiderFlashNewsModel ToFlashNewsModel(string itemJson)
    {
        return new SpiderFlashNewsModel
        {
            FromMedia = (int)FromTypeOfNews.Jin10Media,
            Category = Jin10NewsResource.FlashCategoryNumber,
            NewsUrl = NewsUrl,
            NewsTime = NewsTime,
            Title = DisplayTitle,
            Content = EffectiveContent,
            Keyword = string.Join(",", Tags),
            Level = FlashLevel,
            StockList = null,
            // 金十图片地址带尺寸后缀 ( 实测形态 : .../demo.png/lite ) , 图片名场景已不适用单列 , 直接存原地址数组
            ImageUrls = string.IsNullOrEmpty(Pic) ? null : JsonUtil.GetJson(new List<string> { Pic }),
            RawContent = itemJson
        };
    }

    /// <summary>
    ///     【标题】正文 → 取标题 ; 否则用正文前若干字兜底
    /// </summary>
    private static string ExtractTitle(string content)
    {
        var text = content.Trim();
        if (text.StartsWith('【'))
        {
            var end = text.IndexOf('】');
            if (end > 1) return text[1..end];
        }

        return text.Length <= TitleMaxLength ? text : text[..TitleMaxLength];
    }

    private static List<string> ReadStringArray(JsonNode? node)
    {
        if (node is not JsonArray array) return [];
        return array.Select(item => item?.ToString() ?? "").Where(item => item != "").ToList();
    }
}
