using KSpider.Json;
using KSpider.Time;
using KSpider.Model;
using KSpider.Tool.Http;

namespace KSpider.Spider.DfNews.Model;

// 单条新闻解析后的完整内容
public class DfContentInfo
{
    public string? NewsTitle { get; set; } // 标题

    public string? NewsSummary { get; set; } // 摘要

    public string? NewsTime { get; set; } // 新闻添加时间
    public string? NewsFrom { get; set; } // 新闻原始来源
    public List<DfContextDetailInfo> NewsDataContent { get; set; } = new(); // 内容结构化片段
    public string? NewsDataContentText { get; set; } // 内容纯文本
    public string? NewsUrl { get; set; } // 新闻原始url

    public string? NewsKeyword { get; set; } // 新闻关键字

    public List<HtmlImageTools.ImgInfo> ImgInfos { get; set; } = new(); // 新闻原始图片信息

    public SpiderNewsContentModel ToSpiderNewsContentModel()
    {
        var model = new SpiderNewsContentModel
        {
            NewsUrl = NewsUrl,
            NewsTitle = NewsTitle,
            NewsSummary = NewsSummary,
            NewsFrom = NewsFrom,
            NewsTime = TimeTools.GetDateByTimeStrForFormat(NewsTime ?? "", TimeTools.TimeFormatForBackSlash),
            NewsKeyword = NewsKeyword,
            NewsContentText = NewsDataContentText,
            NewsContentJson = JsonUtil.GetJson(NewsDataContent)
        };
        return model;
    }
}

// 单个正文片段 ( 段落/图片/表格/列表 )
public class DfContextDetailInfo
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

// 列表接口的单条新闻
public class DfListInfo
{
    public string NewsUrl { get; set; } = "";
    public string NewsTitle { get; set; } = "";
    public string NewsSummary { get; set; } = "";
    public string NewsTime { get; set; } = "";
    public string NewsFrom { get; set; } = "";
    public FromTypeOfNews FromMedia { get; set; }
    public DateTime NewsDownloadTime { get; set; }

    public int Category { get; set; }

    public SpiderNewsListModel ToSpiderNewListModel()
    {
        return new SpiderNewsListModel
        {
            FromMedia = (int)FromTypeOfNews.DfMedia,
            NewsUrl = NewsUrl,
            NewsTitle = NewsTitle,
            NewsSummary = NewsSummary,
            NewsFrom = NewsFrom,
            NewsTime = TimeTools.GetDateByTimeStrForFormat(NewsTime ?? "", TimeTools.TimeFormatForStrikethrough),
            NewsDownloadTime = NewsDownloadTime,
            Category = Category
        };
    }
}

// 文章原始 JSON 响应
public class DfNewsContentOrigin
{
    public string NewsUrl { get; set; } = "";
    public NewsContentOriginType OriginType { get; set; }
    public string NewsOriginContent { get; set; } = "";
    public NewsContentOriginStatus Status { get; set; }
    public string Message { get; set; } = "";

    public SpiderNewsContentOriginModel ToSpiderNewsContentOriginModel()
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
