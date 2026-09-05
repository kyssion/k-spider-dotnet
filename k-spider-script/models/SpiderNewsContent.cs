using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace k_spider_script.models;

/// <summary>
/// 新闻信息详情表
/// </summary>
[Table("spider_news_content")]
[Index("NewsUrl", Name = "uk_news_content_test_url", IsUnique = true)]
public partial class SpiderNewsContent
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("create_time", TypeName = "timestamp without time zone")]
    public DateTime CreateTime { get; set; }

    [Column("update_time", TypeName = "timestamp without time zone")]
    public DateTime UpdateTime { get; set; }

    [Column("news_url")]
    [StringLength(300)]
    public string? NewsUrl { get; set; }

    [Column("news_title")]
    [StringLength(300)]
    public string? NewsTitle { get; set; }

    [Column("news_summary")]
    public string? NewsSummary { get; set; }

    [Column("news_from")]
    [StringLength(100)]
    public string? NewsFrom { get; set; }

    [Column("news_time", TypeName = "timestamp without time zone")]
    public DateTime? NewsTime { get; set; }

    [Column("news_keyword")]
    public string? NewsKeyword { get; set; }

    [Column("news_content_json")]
    public string? NewsContentJson { get; set; }

    [Column("news_content_text")]
    public string? NewsContentText { get; set; }
}
