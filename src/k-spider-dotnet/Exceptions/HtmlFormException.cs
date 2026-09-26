namespace KSpider.Exceptions;

/// <summary>
///     各源爬虫的抓取 / 解析失败通用包装 : 响应不是预期形态 ( 反爬拦下返回 HTML 页 / 非法 JSON /
///     errno 非 0 / 缺关键字段 ) 或解析抛错时抛出 , 由 Job 层按这条数据失败记入状态机
/// </summary>
public class HtmlFormException : DownloadHttpException
{
    public HtmlFormException(string newsUrl, string message) : base(newsUrl, message)
    {
    }

    public HtmlFormException(string newsUrl, string message, Exception innerException) : base(newsUrl, message,
        innerException)
    {
    }
}