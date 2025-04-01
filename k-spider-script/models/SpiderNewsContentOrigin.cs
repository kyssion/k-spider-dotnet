using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace k_spirder_script.models;

[Table("spider_news_content_origin")]
[Index("NewsUrl", Name = "uk_news_content_origin_url", IsUnique = true)]
public partial class SpiderNewsContentOrigin
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
    public string NewsUrl { get; set; } = null!;

    [Column("news_origin_content")]
    public string? NewsOriginContent { get; set; }

    [Column("news_origin_type")]
    public int NewsOriginType { get; set; }

    [Column("status")]
    public int Status { get; set; }

    [Column("message")]
    public string? Message { get; set; }
}
