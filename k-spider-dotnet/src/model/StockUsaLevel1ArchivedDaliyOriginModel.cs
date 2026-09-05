using SqlSugar;

namespace k_spider_dotnet.model;

/// <summary>
///     美股股市信息天级别level1归档原始数据
/// </summary>
[SugarTable("stock_usa_level1_archived_daily_origin")]
public class StockUsaLevel1ArchivedDailyOriginModel
{
    /// <summary>
    ///     Desc:
    ///     Default:nextval('stock_usa_level1_archived_daliy_id_seq'::regclass)
    ///     Nullable:False
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
    public long Id { get; set; }

    /// <summary>
    ///     Desc:
    ///     Default:DateTime.Now
    ///     Nullable:False
    /// </summary>
    [SugarColumn(ColumnName = "create_time")]
    public DateTime CreateTime { get; set; }

    /// <summary>
    ///     Desc:
    ///     Default:DateTime.Now
    ///     Nullable:False
    /// </summary>
    [SugarColumn(ColumnName = "update_time")]
    public DateTime UpdateTime { get; set; }

    /// <summary>
    ///     Desc:
    ///     Default:
    ///     Nullable:False
    /// </summary>
    [SugarColumn(ColumnName = "stock_id")]
    public string StockId { get; set; }

    /// <summary>
    ///     Desc:
    ///     Default:
    ///     Nullable:False
    /// </summary>
    [SugarColumn(ColumnName = "exchange_channel")]
    public int ExchangeChannel { get; set; }

    /// <summary>
    ///     Desc:
    ///     Default:
    ///     Nullable:False
    /// </summary>
    [SugarColumn(ColumnName = "date")]
    public DateTime Date { get; set; }

    /// <summary>
    ///     Desc:
    ///     Default:
    ///     Nullable:True
    /// </summary>
    [SugarColumn(ColumnName = "archived")]
    public string? Archived { get; set; }

    /// <summary>
    ///     Desc:
    ///     Default:
    ///     Nullable:True
    /// </summary>
    [SugarColumn(ColumnName = "data_from")]
    public int? DataFrom { get; set; }
}