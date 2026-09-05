using k_spider_dotnet_lib.json;
using k_spider_dotnet_lib.time;
using k_spider_dotnet.model;
using k_spider_dotnet.tool.http;

namespace k_spider_dotnet.spider.df_news.model;

public class DfSpiderModel
{
}

// 单条新闻解析后的完整内容 ( 携带集合属性 , 使用 class 避免结构体拷贝语义陷阱 )
public class DfContentInfo
{
    public string? NewsTitle { get; set; } // 标题 

    public string? NewsSummary { get; set; } // 摘要

    // 新闻添加时间
    public string? NewsTime { get; set; } // 新闻添加时间
    public string? NewsFrom { get; set; } // 新闻原始来源
    public List<DfContextDetailInfo> NewsDataContent { get; set; } // DataContext 内容序列化结构
    public string? NewsDataContentText { get; set; } // DataContextAll 内容文本序列化结构
    public string? NewsUrl { get; set; } // 新闻原始url

    public string? NewsKeyword { get; set; } // 新闻关键字

    public List<HtmlImageTools.ImgInfo> ImgInfos { get; set; } // 新闻原始图片信息

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

public struct DfContextDetailInfo
{
    public string? TagType { get; set; } // 原始结构标签
    public string? Value { get; set; } // 原始结构文本内容
    public string? ValueType { get; set; } // 原始结构 内容类型

    public string? ResourceUri { get; set; } // 如果是图片等资源的 Uri地址
}

public struct DfListInfo
{
    public string NewsUrl { get; set; }
    public string NewsTitle { get; set; }
    public string NewsSummary { get; set; }
    public string NewsTime { get; set; }
    public string NewsFrom { get; set; }
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

public struct DfNewsContentOrigin
{
    public string NewsUrl { get; set; }
    public NewsContentOriginType OriginType { get; set; }
    public string NewsOriginContent { get; set; }
    public NewsContentOriginStatus Status { get; set; }
    public string Message { get; set; }

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