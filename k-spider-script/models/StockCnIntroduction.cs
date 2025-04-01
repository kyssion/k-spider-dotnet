using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace k_spirder_script.models;

[Table("stock_cn_introduction")]
[Index("StockId", Name = "uk_stock_cn_introduction", IsUnique = true)]
public partial class StockCnIntroduction
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
    public string? StockId { get; set; }

    [Column("stock_name")]
    [StringLength(200)]
    public string? StockName { get; set; }

    [Column("exchange_channel")]
    public int? ExchangeChannel { get; set; }
}
