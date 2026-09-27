namespace KSpider.Exceptions;

/// <summary>
///     数据库操作异常包装 ( DAO 层统一抛出 )。Job 层捕获它时不消耗 fail_count :
///     数据库故障属基础设施问题 , 保持原状态等下一轮自然重试 , 与业务性失败 ( 消耗重试次数 ) 区分
/// </summary>
public class KDbException : Exception
{
    public KDbException(string message) : base(message)
    {
    }

    public KDbException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
