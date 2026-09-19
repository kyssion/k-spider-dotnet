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
///     一页列表结果 : 列表项 + 可选的内联原始内容 + 下一页游标
/// </summary>
public class NewsListPage
{
    public List<SpiderNewsListModel> Items { get; init; } = [];

    /// <summary>
    ///     列表接口已带全文的源 ( 快讯型 ) 在此同时给出原始内容 , 由列表任务与列表行同一事务落库
    ///     需要详情页的源保持为空
    /// </summary>
    public List<NewsContentOrigin> InlineOrigins { get; init; } = [];

    /// <summary>
    ///     下一页游标 , null = 没有更多 ; 游标对任务不透明 , 由各源自行解释 ( 东财 = 页码 , 财联社 = last_time )
    /// </summary>
    public string? NextCursor { get; init; }
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
///     单个正文片段 ( 段落 / 图片 / 表格 / 列表 ) , 各源的 news_content_json 共用同一份结构
/// </summary>
public class NewsContentSegment
{
    /// <summary>
    ///     内容类型常量 , 保持字符串形式以兼容已入库的 news_content_json 数据
    /// </summary>
    public const string TextType = "TEXT";
    public const string ImgType = "IMG";
    public const string TableType = "TABLE";
    public const string UlType = "UL";
    public const string OtherType = "OTHER";

    public string? TagType { get; set; } // 原始结构标签
    public string? Value { get; set; } // 原始结构文本内容
    public string? ValueType { get; set; } // 内容类型 ( 上述常量 )

    public string? ResourceUri { get; set; } // 如果是图片等资源的 Uri地址
}

/// <summary>
///     解析结果 : 详情实体 + 图片列表实体
/// </summary>
public class NewsContentParseResult
{
    public SpiderNewsContentModel Content { get; init; } = new();

    public List<SpiderNewsImageListModel> Images { get; init; } = [];
}
