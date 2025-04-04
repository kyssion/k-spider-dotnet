using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace k_spider_script.models;

/// <summary>
/// 爬虫详情中的图片信息记录
/// </summary>
[Table("spider_news_image_list")]
[Index("ImageResourceUrl", Name = "uk_news_image_url", IsUnique = true)]
public partial class SpiderNewsImageList
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

    [Column("image_resource_url")]
    [StringLength(400)]
    public string? ImageResourceUrl { get; set; }

    [Column("image_name")]
    [StringLength(100)]
    public string? ImageName { get; set; }
}
