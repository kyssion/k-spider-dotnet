using KSpider.Common.Json;
using KSpider.Spider.News.Web;
using KSpider.Common.Time;
using KSpider.Model;
using KSpider.Tool.Http;

namespace KSpider.Spider.News.Web.Eastmoney.Model;

// 单条新闻解析后的完整内容
public class DfContentInfo
{
    public string? NewsTitle { get; set; } // 标题

    public string? NewsSummary { get; set; } // 摘要

    public string? NewsTime { get; set; } // 新闻添加时间
    public string? NewsFrom { get; set; } // 新闻原始来源
    public List<NewsContentSegment> NewsDataContent { get; set; } = new(); // 内容结构化片段
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
