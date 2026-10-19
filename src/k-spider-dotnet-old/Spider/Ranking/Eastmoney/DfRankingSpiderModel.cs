using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using KSpider.Model;
using KSpider.Spider;

namespace KSpider.Spider.Ranking.Eastmoney;

/// <summary>
///     盘面榜单列表页 : 条目 + 分页信息 ( datacenter 接口 result.pages 总页数 )
/// </summary>
public sealed record DfRankingPage(List<SpiderRankingModel> Items, int TotalPage)
{
    /// <summary>是否还有下一页</summary>
    public bool HasNextPage(int pageNo) => pageNo < TotalPage;
}

/// <summary>
///     榜单行解析 : datacenter 各 reportName 的 data[] 项 → 落库模型。
///     通用列 ( 代码/名称/收盘/涨跌幅/成交额/净额/买卖额 ) 按类型填充 ,
///     类型特有长尾进 detail ( JSONB ) ; row_key 按类型组装自然键 ( 契约第 2 条 )。
/// </summary>
public static class DfRankingItem
{
    public static SpiderRankingModel? FromJson(JsonNode node, RankingType type)
    {
        var code = Text(node, "SECURITY_CODE") ?? Text(node, "SCODE");
        var dateText = Text(node, "TRADE_DATE") ?? Text(node, "DATE");
        if (string.IsNullOrEmpty(code) ||
            !DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var tradeDate))
            return null;

        var model = new SpiderRankingModel
        {
            FromMedia = (int)FromTypeOfNews.DfMedia,
            RankingType = (short)type,
            TradeDate = DateTime.SpecifyKind(tradeDate, DateTimeKind.Unspecified),
            StockCode = code,
            StockName = Text(node, "SECURITY_NAME_ABBR") ?? Text(node, "SECNAME"),
            Market = Text(node, "MARKET") ?? Text(node, "TRADE_MARKET_OLD"),
            ClosePrice = Dec(node, "CLOSE_PRICE"),
            ChangeRate = Dec(node, "CHANGE_RATE"),
            RawContent = node.ToJsonString()
        };

        switch (type)
        {
            case RankingType.Lhb:
                // 龙虎榜 : 自然键 = 代码 + 上榜原因 + 稳定数值指纹
                // ( 同股同日同原因存在多行 , 席位解释等文案会更新故指纹只取数值列 )
                model.DealAmount = Dec(node, "BILLBOARD_DEAL_AMT");
                model.NetAmount = Dec(node, "BILLBOARD_NET_AMT");
                model.BuyAmount = Dec(node, "BILLBOARD_BUY_AMT");
                model.SellAmount = Dec(node, "BILLBOARD_SELL_AMT");
                model.RowKey = $"{code}|{Text(node, "CHANGE_TYPE") ?? ""}" +
                               Fingerprint(model.DealAmount, model.NetAmount, model.BuyAmount, model.SellAmount);
                break;

            case RankingType.BlockTrade:
                // 大宗 : 同股同日同买卖方同价仍可能多笔 ( 分手成交 ) , 自然键 = 业务键 + 量额指纹
                model.DealAmount = Dec(node, "DEAL_AMT");
                model.RowKey = $"{code}|{Text(node, "BUYER_NAME") ?? ""}|{Text(node, "SELLER_NAME") ?? ""}|{Text(node, "DEAL_PRICE") ?? ""}" +
                               Fingerprint(Dec(node, "DEAL_VOLUME"), model.DealAmount);
                break;

            case RankingType.Margin:
                // 两融 : 每股每日一行 ( 沪深市场互斥 ) , 自然键 = 代码
                model.RowKey = code;
                break;

            default:
                return null; // 预留类型 ( 北向 ) 无解析实现
        }

        model.Detail = node.ToJsonString();
        return model;
    }

    /// <summary>
    ///     稳定字段子集指纹 ( SHA1 前 8 位 hex ) : 追加到行键消除同业务键多笔的碰撞 ,
    ///     只取数值列——文案列 ( 席位解释等 ) 会更新 , 进指纹会让同一行重复入库
    /// </summary>
    private static string Fingerprint(params decimal?[] values)
    {
        var seed = string.Join("|", values.Select(value => value?.ToString(CultureInfo.InvariantCulture) ?? "-"));
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(seed));
        return Convert.ToHexString(hash, 0, 4);
    }

    /// <summary>字符串字段 : 缺失或空串归一为 null</summary>
    private static string? Text(JsonNode node, string key)
    {
        var value = node[key]?.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>数值字段 : 空串或非数值归一为 null</summary>
    private static decimal? Dec(JsonNode node, string key)
    {
        var value = node[key]?.ToString();
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;
    }
}
