using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using KSpider.Exceptions;
using KSpider.Spider.Verify;
using KSpider.Common;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.Announcement.Cninfo;

/// <summary>
///     巨潮资讯公告源 ( Announcement 管线 ) : POST hisAnnouncement/query 公开 JSON ,
///     列表即文档元数据直写 , 无正文无解析二段。
///     独立于新闻注册表 , 由 AnnouncementJob 直连。
/// </summary>
public class CninfoAnnouncementSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<CninfoAnnouncementSpider>();

    private static readonly JsonSerializerOptions SnakeCaseOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    ///     拉一页公告 ( 日期窗 + 分类白名单 + 页码 ; POST 表单编码 )
    /// </summary>
    public async Task<CninfoAnnouncementPage> GetAnnouncementPage(DateTime beginDate, DateTime endDate,
        int pageNo)
    {
        var form = new Dictionary<string, string>
        {
            ["pageNum"] = pageNo.ToString(),
            ["pageSize"] = CninfoAnnouncementResource.MaxPageSize.ToString(),
            ["column"] = "",
            ["tabName"] = CninfoAnnouncementResource.TabName,
            ["category"] = string.Join(";", CninfoAnnouncementResource.AnnouncementCategoryList),
            ["seDate"] = $"{beginDate:yyyy-MM-dd}~{endDate:yyyy-MM-dd}",
            ["isHLtitle"] = "true"
        };
        var url = CninfoAnnouncementResource.QueryUrl;
        try
        {
            var responseString = await VerifiedHttp.SendStringAsync(CninfoAnnouncementResource.ResourceHost,
                () => new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new FormUrlEncodedContent(form)
                });
            return ParseAnnouncementPage(responseString);
        }
        catch (Exception e)
        {
            var message =
                $"[CninfoAnnouncementSpider GetAnnouncementPage] 拉取公告失败 , window : {beginDate:yyyy-MM-dd}~{endDate:yyyy-MM-dd} , pageNo : {pageNo} , err : {e}";
            Log.LogError(message);
            throw new HtmlFormException(url, message, e);
        }
    }

    /// <summary>
    ///     解析公告响应 ( 独立成公开静态方法供离线测试 ) :
    ///     announcements 缺失 / 无效显式报错 ; hasMore 表示还有下一页
    /// </summary>
    public static CninfoAnnouncementPage ParseAnnouncementPage(string responseString)
    {
        JsonNode? jsonNode;
        try
        {
            jsonNode = JsonNode.Parse(responseString);
        }
        catch (JsonException e)
        {
            throw new HtmlFormException(CninfoAnnouncementResource.QueryUrl,
                $"[CninfoAnnouncementSpider ParseAnnouncementPage] 响应不是合法 JSON : {e.Message}");
        }

        if (jsonNode == null)
            throw new HtmlFormException(CninfoAnnouncementResource.QueryUrl,
                "[CninfoAnnouncementSpider ParseAnnouncementPage] 响应为空");
        if (jsonNode["announcements"] is not JsonArray dataArray)
            throw new HtmlFormException(CninfoAnnouncementResource.QueryUrl,
                "[CninfoAnnouncementSpider ParseAnnouncementPage] 响应缺少 announcements 数组");

        var items = new List<KSpider.Model.SpiderAnnouncementModel>();
        foreach (var node in dataArray)
        {
            if (node == null) continue;
            var item = CninfoAnnouncementItem.FromJson(node);
            if (item != null) items.Add(item);
        }

        var hasMore = jsonNode["hasMore"]?.GetValue<bool?>() ?? false;
        return new CninfoAnnouncementPage(items, hasMore);
    }
}
