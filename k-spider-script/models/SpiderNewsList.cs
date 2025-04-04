using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace k_spider_script.models;

/// <summary>
/// 排重抓取信息信息列表
/// </summary>
[Table("spider_news_list")]
[Index("NewsUrl", Name = "uk_news_url", IsUnique = true)]
public partial class SpiderNewsList
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("create_time", TypeName = "timestamp without time zone")]
    public DateTime CreateTime { get; set; }

    [Column("update_time", TypeName = "timestamp without time zone")]
    public DateTime UpdateTime { get; set; }

    [Column("from_media")]
    public int? FromMedia { get; set; }

    [Column("news_url")]
    [StringLength(300)]
    public string? NewsUrl { get; set; }

    [Column("news_title")]
    [StringLength(300)]
    public string? NewsTitle { get; set; }

    [Column("news_summary")]
    public string? NewsSummary { get; set; }

    [Column("news_from")]
    [StringLength(30)]
    public string? NewsFrom { get; set; }

    [Column("news_time", TypeName = "timestamp without time zone")]
    public DateTime? NewsTime { get; set; }

    [Column("news_download_time", TypeName = "timestamp without time zone")]
    public DateTime? NewsDownloadTime { get; set; }

    /// <summary>
    /// 新闻类型
    /// </summary>
    [Column("category")]
    public int Category { get; set; }

    /// <summary>
    /// 详情数据是否下载 0 没有下载 1 已下载
    /// </summary>
    [Column("download_status_code")]
    public int DownloadStatusCode { get; set; }
}
