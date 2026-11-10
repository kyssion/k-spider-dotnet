namespace KSpider.Admin.Repository.Exceptions;

/// <summary>
///     数据访问异常边界。
/// </summary>
/// <remarks>
///     调用方应把 <see cref="KDbException" /> 与“数据本身失败”分开：前者表示数据库不可达、
///     SQL 失败或映射异常，不应消耗业务重试计数；后者由任务状态机按 run_failed 处理。
/// </remarks>
public sealed class KDbException : Exception
{
    /// <summary>
    ///     包装底层数据库异常。
    /// </summary>
    /// <param name="message">面向日志的错误描述。</param>
    /// <param name="innerException">原始异常。</param>
    public KDbException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
