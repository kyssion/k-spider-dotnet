using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using KSpider.Model;
using KSpider.Spider;

namespace KSpider.Spider.Dataset.LootDrop;

/// <summary>
///     PostgREST startups 行 → 实体 :
///     与各源列表项同款约定 , 在 FromJson 一次性显式赋值 ;
///     字符串一律 Trim ( 实测存在 "closed\r\n" 这类脏尾巴 ) , 数字与时间空值安全 ;
///     JSON 数组/对象列 ( founders/investors/the_loot/pivot_idea ) 原样序列化为 JSON 文本入库 ;
///     ContentHash 在解析期即算好 ( 全部业务字段的规范化拼接 SHA256 , 见 ComputeContentHash )。
/// </summary>
public class LootDropDatasetRow
{
    public static SpiderLootDropStartupModel FromJson(JsonNode node)
    {
        var id = node["id"]?.GetValue<long>() ?? 0;
        var model = new SpiderLootDropStartupModel
        {
            Id = id,
            FromMedia = (int)FromTypeOfNews.LootDropMedia,
            Name = ReadString(node, "name") ?? "",
            Description = ReadString(node, "description"),
            Sector = ReadString(node, "sector"),
            Country = ReadString(node, "country"),
            Founders = ReadRawJson(node, "founders"),
            Investors = ReadRawJson(node, "investors"),
            StartYear = ReadInt(node, "start_year"),
            EndYear = ReadInt(node, "end_year"),
            TotalFunding = ReadLong(node, "total_funding"),
            Difficulty = ReadInt(node, "difficulty"),
            DifficultyReason = ReadString(node, "difficulty_reason"),
            Scalability = ReadInt(node, "scalability"),
            ScalabilityReason = ReadString(node, "scalability_reason"),
            MarketPotential = ReadString(node, "market_potential"),
            MarketPotentialReason = ReadString(node, "market_potential_reason"),
            CauseOfDeath = ReadString(node, "cause_of_death"),
            TheLoot = ReadRawJson(node, "the_loot"),
            MarketAnalysis = ReadString(node, "market_analysis"),
            PivotIdea = ReadRawJson(node, "pivot_idea"),
            SourceCreatedAt = ReadUtcTimestamp(node, "created_at"),
            Status = ReadString(node, "status"),
            PrimaryCauseOfDeath = ReadString(node, "primary_cause_of_death"),
            ProductType = ReadString(node, "product_type"),
            Views = ReadInt(node, "views"),
            CondensedValueProp = ReadString(node, "condensed_value_prop"),
            CondensedCauseOfDeath = ReadString(node, "condensed_cause_of_death"),
            RawContent = node.ToJsonString()
        };
        model.ContentHash = ComputeContentHash(model);
        return model;
    }

    /// <summary>
    ///     行内容指纹 : 全部业务字段的规范化拼接 SHA256 ( 小写十六进制 )。
    ///     参与字段 = 除 id / from_media / content_hash / raw_content / create_time / update_time 外的全部列 ;
    ///     空值统一空串 , 时间按不变文化格式化 —— 同内容必同哈希 ( 稳定性 ) , 任一字段变必不同哈希 ( 敏感性 )。
    ///     upsert 以它做变更检测 , 只有指纹变化的行才被 UPDATE。
    /// </summary>
    public static string ComputeContentHash(SpiderLootDropStartupModel model)
    {
        string Part(string? value) => value ?? "";
        string PartInt(int? value) => value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
        string PartLong(long? value) => value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
        var joined = string.Join("\n",
            Part(model.Name),
            Part(model.Description),
            Part(model.Sector),
            Part(model.Country),
            Part(model.Founders),
            Part(model.Investors),
            PartInt(model.StartYear),
            PartInt(model.EndYear),
            PartLong(model.TotalFunding),
            PartInt(model.Difficulty),
            Part(model.DifficultyReason),
            PartInt(model.Scalability),
            Part(model.ScalabilityReason),
            Part(model.MarketPotential),
            Part(model.MarketPotentialReason),
            Part(model.CauseOfDeath),
            Part(model.TheLoot),
            Part(model.MarketAnalysis),
            Part(model.PivotIdea),
            model.SourceCreatedAt?.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) ?? "",
            Part(model.Status),
            Part(model.PrimaryCauseOfDeath),
            Part(model.ProductType),
            PartInt(model.Views),
            Part(model.CondensedValueProp),
            Part(model.CondensedCauseOfDeath));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(joined)));
    }

    /// <summary>字符串取值并 Trim ( 源侧存在 "closed\r\n" 脏尾巴 ) ; 缺失返回 null</summary>
    private static string? ReadString(JsonNode node, string field)
    {
        var value = node[field]?.ToString();
        return value == null ? null : value.Trim();
    }

    /// <summary>JSON 数组/对象列原样序列化 ( 保留源结构 , 缺失返回 null )</summary>
    private static string? ReadRawJson(JsonNode node, string field)
    {
        return node[field]?.ToJsonString();
    }

    private static int? ReadInt(JsonNode node, string field)
    {
        return node[field] is JsonValue value && value.TryGetValue<int>(out var number) ? number : null;
    }

    private static long? ReadLong(JsonNode node, string field)
    {
        return node[field] is JsonValue value && value.TryGetValue<long>(out var number) ? number : null;
    }

    /// <summary>
    ///     源侧 ISO8601 时间 ( 如 2026-01-26T21:07:20.221+00:00 ) → UTC 后去时区种 :
    ///     列为 timestamp without time zone , 统一存 UTC 纯时间戳 ( 规避 Npgsql 时区编码坑 , 见 AGENTS.md 坑 19 )
    /// </summary>
    private static DateTime? ReadUtcTimestamp(JsonNode node, string field)
    {
        var text = node[field]?.ToString();
        if (string.IsNullOrEmpty(text)) return null;
        if (!DateTimeOffset.TryParse(text, out var parsed)) return null;
        return DateTime.SpecifyKind(parsed.UtcDateTime, DateTimeKind.Unspecified);
    }
}
