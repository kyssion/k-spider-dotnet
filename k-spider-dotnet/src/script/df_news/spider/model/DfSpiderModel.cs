using k_spider_dotnet.model;
using k_spider_dotnet.tool.html;
using k_spider_dotnet.tool.http;
using k_spider_dotnet.tool.resource;
using k_spider_dotnet.tool.time;

namespace k_spider_dotnet.script.df_news.spider.model;

public class DfSpiderModel
{
    
}

public struct DfContextInfo
{
    public string? Title { get; set; } // 标题

    public string? AbstractInfo { get; set; } // 摘要

    // 新闻添加时间
    public string? NewsTime { get; set; } // 新闻添加时间
    public string? NewsFrom { get; set; } // 新闻原始来源
    public List<DfContextDetailInfo> DataContext { get; set; } // DataContext 内容序列化结构
    public string? DataContextAll { get; set; } // DataContextAll 内容文本序列化结构
    public string? FromUrl { get; set; } // 新闻原始url
        
    public List<HtmlGetImgDownLoad.ImgInfo> ImgInfos { get; set; } // 新闻原始图片信息
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
    public NewsFromType FromMedia { get; set; }
    public DateTime NewsDownloadTime { get; set; }
    
    public int Category { get; set; }

    public SpiderNewsListModel ToSpiderNewListModel()
    {
        return new SpiderNewsListModel
        {
            FromMedia = (int)NewsFromType.DfMedia,
            NewsUrl = this.NewsUrl,
            NewsTitle = this.NewsTitle,
            NewsSummary = this.NewsSummary,
            NewsFrom = this.NewsFrom,
            NewsTime = TimeTools.GetDateByTimeStr(this.NewsTime ?? "", TimeTools.DfTimeFormat),
            NewsDownloadTime = this.NewsDownloadTime,
        };
    }
}
