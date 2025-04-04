using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace k_spider_script.models;

/// <summary>
/// 香港股市信息天级别level1原始数据
/// </summary>
[Table("stock_hk_level1_archived_daily_origin")]
[Index("Date", "StockId", Name = "uk_hk_daily_stock", IsUnique = true)]
public partial class StockHkLevel1ArchivedDailyOrigin
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("create_time", TypeName = "timestamp without time zone")]
    public DateTime CreateTime { get; set; }

    [Column("update_time", TypeName = "timestamp without time zone")]
    public DateTime UpdateTime { get; set; }

    [Column("stock_id")]
    [StringLength(200)]
    public string StockId { get; set; } = null!;

    [Column("exchange_channel")]
    public int ExchangeChannel { get; set; }

    [Column("date")]
    public DateOnly Date { get; set; }

    [Column("archived")]
    public string? Archived { get; set; }

    [Column("data_from")]
    public int? DataFrom { get; set; }
}
