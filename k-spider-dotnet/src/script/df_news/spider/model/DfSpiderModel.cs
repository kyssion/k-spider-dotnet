using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using k_spider_dotnet.model;
using k_spider_dotnet.tool.http;
using k_spider_dotnet.tool.Json;
using k_spider_dotnet.tool.resource;
using k_spider_dotnet.tool.time;

namespace k_spider_dotnet.script.df_news.spider.model;

public class DfSpiderModel
{
}

public struct DfContentInfo
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

    public List<HtmlGetImgDownLoad.ImgInfo> ImgInfos { get; set; } // 新闻原始图片信息
    
    public SpiderNewsContentTestModel ToSpiderNewsContentTestModel()
    {
        var model =  new SpiderNewsContentTestModel
        {
            NewsUrl = this.NewsUrl,
            NewsTitle = this.NewsTitle,
            NewsSummary = this.NewsSummary,
            NewsFrom = this.NewsFrom,
            NewsTime = TimeTools.GetDateByTimeStrForFormat(this.NewsTime ?? "", TimeTools.TimeFormatForBackSlash),
            NewsKeyword = this.NewsKeyword,
            NewsContentText = this.NewsDataContentText,
            NewsContentJson = JsonUtil.GetJson(this.NewsDataContent)
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
            NewsTime = TimeTools.GetDateByTimeStrForFormat(this.NewsTime ?? "", TimeTools.TimeFormatForStrikethrough),
            NewsDownloadTime = this.NewsDownloadTime,
            Category = this.Category
        };
    }
}