using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.Json.Nodes;
using KSpider.Model;
using KSpider.Spider;

namespace KSpider.Spider.Announcement.Cninfo;

/// <summary>
///     巨潮公告列表页 : 条目 + 是否还有更多 ( hasMore )
/// </summary>
public sealed record CninfoAnnouncementPage(List<SpiderAnnouncementModel> Items, bool HasMore);

/// <summary>
///     巨潮公告的单条数据 ( hisAnnouncement/query 的 announcements 项 ) :
///     行即文档元数据 ( 证券 + 标题 + 分类代码 + PDF 链接 ) , 无正文 ;
///     announcementId 站内唯一为去重键 ; announcementTime 为 unix 毫秒 ;
///     announcementType 为分类代码串 ( "01010503||010112||…" 原样入库 , 对照巨潮分类表解读 )。
/// </summary>
public static class CninfoAnnouncementItem
{
    public static SpiderAnnouncementModel? FromJson(JsonNode node)
    {
        var announcementId = node["announcementId"]?.ToString();
        var title = node["announcementTitle"]?.ToString();
        // 无 id 或无标题视为坏数据 ; 公告偶发无证券主体 ( 基金/债券类 ) , 证券列可空不跳过
        if (string.IsNullOrWhiteSpace(announcementId) || string.IsNullOrWhiteSpace(title)) return null;
        // 标题带 <em> 高亮标签 ( isHLtitle=true ) , 剥掉
        title = HighlightRegex.Replace(title, "").Trim();

        var model = new SpiderAnnouncementModel
        {
            FromMedia = (int)FromTypeOfNews.CninfoMedia,
            AnnouncementId = announcementId,
            Title = title,
            SecCode = Text(node["secCode"]),
            SecName = Text(node["secName"]),
            Category = Text(node["announcementType"]),
            RawContent = node.ToJsonString()
        };

        var adjunct = Text(node["adjunctUrl"]);
        model.PdfUrl = string.IsNullOrEmpty(adjunct) ? null : CninfoAnnouncementResource.PdfUrlPrefix + adjunct;

        // announcementTime 为 unix 毫秒
        if (long.TryParse(node["announcementTime"]?.ToString(), out var millis) && millis > 0)
            model.PublishTime = DateTimeOffset.FromUnixTimeMilliseconds(millis)
                .ToOffset(TimeSpan.FromHours(8)).DateTime;

        return model;
    }

    private static string? Text(JsonNode? node)
    {
        var value = node?.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static readonly Regex HighlightRegex = new(@"</?em>", RegexOptions.Compiled);
}
