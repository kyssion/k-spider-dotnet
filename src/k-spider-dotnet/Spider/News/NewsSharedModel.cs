namespace KSpider.Spider.News;

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
