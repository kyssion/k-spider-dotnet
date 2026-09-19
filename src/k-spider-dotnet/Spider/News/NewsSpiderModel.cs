namespace KSpider.Spider.News.Web;

/// <summary>
///     源内部的一个栏目。
///     ColumnId 是**源内部**的栏目标识 , 用于日志与健康检查定位 ( 东财为列表接口的 column 号 )。
///     注意 : 它只作标识 , 当前各源取数用的频道 / 栏目参数仍写在各源自己的 Resource 常量里 ——
///     两者的取值不保证一致 ( 例如见闻 ColumnId 是 "global" , 而接口参数要 "global-channel" )。
/// </summary>
public sealed class NewsColumn(string columnId, string columnName)
{
    /// <summary>
    ///     源内部栏目标识 ( 同时用于日志与健康检查定位 )
    /// </summary>
    public string ColumnId { get; } = columnId;

    public string ColumnName { get; } = columnName;
}

/// <summary>
///     单个正文片段 ( 段落 / 图片 / 表格 / 列表 ) , 网页型与快讯型源共用的 news_content_json 结构
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
