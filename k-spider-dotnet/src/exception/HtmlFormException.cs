namespace k_spider_dotnet.exception;

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