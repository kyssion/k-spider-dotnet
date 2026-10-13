using System.Text.Json;
using System.Text.Json.Nodes;
using KSpider.Common;
using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider.Verify;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.Dataset.LootDrop;

/// <summary>
///     Loot Drop 数据集源 : 直连 Supabase PostgREST 全量拉取 startups 表。
///     与网页抓取型完全不同 —— 无列表页无详情页无解析三段 , REST 即结构化全量 ;
///     有界集合 + 行会修订 , 写入走 content_hash 变更检测 ( 见数据集型契约 )。
///     由 LootDropDatasetJob 直连 ( 不进任何 Spider 注册表 ) ;
///     出现第二个数据集源时再抽注册表。
/// </summary>
public class LootDropDatasetSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<LootDropDatasetSpider>();

    /// <summary>
    ///     全量拉取 : id 升序 offset 翻页 ( 每页一条独立请求 ) ,
    ///     短页即末页 ; MaxRows 上限防翻页失控。
    ///     失败直接上抛 , 由任务层记日志 ( 下一轮自然重试 , 与研报同款语义 )
    /// </summary>
    public async Task<List<SpiderLootDropStartupModel>> GetAllStartupsAsync()
    {
        var all = new List<SpiderLootDropStartupModel>();
        for (var offset = 0; offset < LootDropDatasetResource.MaxRows; offset += LootDropDatasetResource.PageSize)
        {
            var page = await GetStartupsPageAsync(offset, LootDropDatasetResource.PageSize);
            all.AddRange(page);
            if (page.Count < LootDropDatasetResource.PageSize) return all;
        }

        Log.LogWarning("[LootDropDatasetSpider GetAllStartupsAsync] 翻页达 MaxRows 上限 {0} , 疑似游标异常 , 截断",
            LootDropDatasetResource.MaxRows);
        return all;
    }

    /// <summary>拉一页 ( offset 翻页 ; pageSize 供探测/测试传小值 )</summary>
    public async Task<List<SpiderLootDropStartupModel>> GetStartupsPageAsync(int offset, int pageSize)
    {
        var requestSize = Math.Min(pageSize, LootDropDatasetResource.PageSize);
        var url = string.Format(LootDropDatasetResource.StartupsPageUrlTemplate, offset, requestSize);
        try
        {
            var responseString = await VerifiedHttp.SendStringAsync(LootDropDatasetResource.ResourceHost, () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("apikey", LootDropDatasetResource.ApiKey);
                return request;
            });
            return ParseStartups(responseString);
        }
        catch (Exception e)
        {
            var message =
                $"[LootDropDatasetSpider GetStartupsPageAsync] 拉取数据集失败 , offset : {offset} , url : {url} , err : {e}";
            Log.LogError(message);
            throw new HtmlFormException(url, message, e);
        }
    }

    /// <summary>
    ///     远端总行数 ( 对账监控用 ) : HEAD 带 Prefer: count=exact ,
    ///     从响应头 Content-Range ( 如 "0-0/1749" ) 解析总数
    /// </summary>
    public async Task<long> GetRemoteCountAsync()
    {
        try
        {
            return await VerifiedHttp.SendAsync(LootDropDatasetResource.ResourceHost, () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Head, LootDropDatasetResource.CountProbeUrl);
                request.Headers.Add("apikey", LootDropDatasetResource.ApiKey);
                request.Headers.Add("Prefer", "count=exact");
                // Range 是类型化请求头 , 裸 Add("Range", ...) 会抛 FormatException ( Live 用例实测踩过 )
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 0);
                return request;
            }, (response, _, _) =>
            {
                // Content-Range 是 content 头 : 必须从 Content.Headers 读 ( Live 用例实测踩过 )
                var contentRange = response.Content.Headers.Contains("Content-Range")
                    ? string.Join(",", response.Content.Headers.GetValues("Content-Range"))
                    : "";
                var total = ParseContentRangeTotal(contentRange);
                if (total < 0)
                    throw new HtmlFormException(LootDropDatasetResource.CountProbeUrl,
                        $"[LootDropDatasetSpider GetRemoteCountAsync] Content-Range 无法解析 : {contentRange}");
                return Task.FromResult(total);
            });
        }
        catch (HtmlFormException)
        {
            throw;
        }
        catch (Exception e)
        {
            var message =
                $"[LootDropDatasetSpider GetRemoteCountAsync] 对账探测失败 , err : {e}";
            Log.LogError(message);
            throw new HtmlFormException(LootDropDatasetResource.CountProbeUrl, message, e);
        }
    }

    /// <summary>
    ///     解析一页响应 ( 独立成公开静态方法供离线测试 ) :
    ///     响应必须是 JSON 数组 ( PostgREST 行集 ) , 非数组显式报错不当空页 ;
    ///     id/名称缺失的坏行跳过
    /// </summary>
    public static List<SpiderLootDropStartupModel> ParseStartups(string responseString)
    {
        JsonNode? jsonNode;
        try
        {
            jsonNode = JsonNode.Parse(responseString);
        }
        catch (JsonException e)
        {
            throw new HtmlFormException(LootDropDatasetResource.RestBaseUrl,
                $"[LootDropDatasetSpider ParseStartups] 响应不是合法 JSON : {e.Message}");
        }

        if (jsonNode is not JsonArray array)
            throw new HtmlFormException(LootDropDatasetResource.RestBaseUrl,
                $"[LootDropDatasetSpider ParseStartups] 响应不是行集数组 , 原始 : {responseString[..Math.Min(200, responseString.Length)]}");

        var items = new List<SpiderLootDropStartupModel>();
        foreach (var node in array)
        {
            if (node == null) continue;
            var row = LootDropDatasetRow.FromJson(node);
            // id/名称缺失的坏行跳过 ( 与金十列表同款语义 )
            if (row.Id <= 0 || string.IsNullOrEmpty(row.Name)) continue;
            items.Add(row);
        }

        return items;
    }

    /// <summary>解析 Content-Range 尾段总数 ( "0-0/1749" → 1749 ; 无 "/total" 形态返回 -1 )</summary>
    public static long ParseContentRangeTotal(string contentRange)
    {
        var separator = contentRange.LastIndexOf('/');
        if (separator < 0 || separator == contentRange.Length - 1) return -1;
        return long.TryParse(contentRange[(separator + 1)..], out var total) ? total : -1;
    }
}
