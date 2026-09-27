namespace KSpider.Exceptions;

/// <summary>
///     请求 / 下载产物层面的失败特化 ( 网络异常 , 或原始内容形态不符预期需要重新下载 ) ,
///     与解析阶段的 HtmlFormException 用类型区分开 , 便于日志定位问题出在下载还是解析
/// </summary>
public class DownloadHttpRequestException : DownloadHttpException
{
    public DownloadHttpRequestException(string newsUrl, string message) : base(newsUrl, message)
    {
    }

    public DownloadHttpRequestException(string newsUrl, string message, Exception innerException) : base(newsUrl,
        message, innerException)
    {
    }
}
