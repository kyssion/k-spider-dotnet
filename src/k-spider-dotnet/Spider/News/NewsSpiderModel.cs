using KSpider.Model;

namespace KSpider.Spider.News;

/// <summary>
///     源内部的一个栏目
/// </summary>
public sealed class NewsColumn(string columnId, string columnName)
{
    /// <summary>
    ///     源内部栏目标识 ( 东财为列表接口的 column 号 ) , 用于日志与健康检查定位
    /// </summary>
    public string ColumnId { get; } = columnId;

    public string ColumnName { get; } = columnName;
}

/// <summary>
///     单条新闻的原始内容 ( 未解析 )
/// </summary>
public class NewsContentOrigin
{
    public string NewsUrl { get; set; } = "";

    public NewsContentOriginType OriginType { get; set; }

    public string NewsOriginContent { get; set; } = "";

    public NewsContentOriginStatus Status { get; set; }

    public string Message { get; set; } = "";

    public SpiderNewsContentOriginModel ToModel()
    {
        return new SpiderNewsContentOriginModel
        {
            NewsUrl = NewsUrl,
            NewsOriginContent = NewsOriginContent,
            NewsOriginType = (int)OriginType,
            Status = (int)Status,
            Message = Message
        };
    }
}

/// <summary>
///     解析结果 : 详情实体 + 图片列表实体
/// </summary>
public class NewsContentParseResult
{
    public SpiderNewsContentModel Content { get; init; } = new();

    public List<SpiderNewsImageListModel> Images { get; init; } = [];
}
