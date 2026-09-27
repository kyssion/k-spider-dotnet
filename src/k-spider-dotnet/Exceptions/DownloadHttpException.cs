namespace KSpider.Exceptions;

/// <summary>
///     下载阶段失败异常族基类 : 携带出错数据的 news_url ,
///     调用方 ( NewsContentOriginJob / NewsContentJob ) 按"这条数据失败"处理 —— 记失败态并累计 fail_count ,
///     news_url 让日志与状态回写能直接定位是哪条数据
/// </summary>
public class DownloadHttpException : Exception
{
    public DownloadHttpException(string newsUrl, string message) : base(message)
    {
        NewsUrl = newsUrl;
    }

    public DownloadHttpException(string newsUrl, string message, Exception innerException) : base(message,
        innerException)
    {
        NewsUrl = newsUrl;
    }

    public string NewsUrl { get; set; }
}
