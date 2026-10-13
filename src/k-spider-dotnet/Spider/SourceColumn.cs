namespace KSpider.Spider;

/// <summary>
///     源内部的一个栏目 ( 根级共享词汇 , 新闻网页型 / 快讯型 / 官方文件管线共用 )。
///     ColumnId 是**源内部**的栏目标识 , 用于日志与健康检查定位 ( 东财为列表接口的 column 号 )。
///     注意 : 它只作标识 , 当前各源取数用的频道 / 栏目参数仍写在各源自己的 Resource 常量里 ——
///     两者的取值不保证一致 ( 例如见闻 ColumnId 是 "global" , 而接口参数要 "global-channel" )。
/// </summary>
public sealed class SourceColumn(string columnId, string columnName)
{
    /// <summary>
    ///     源内部栏目标识 ( 同时用于日志与健康检查定位 )
    /// </summary>
    public string ColumnId { get; } = columnId;

    public string ColumnName { get; } = columnName;
}
