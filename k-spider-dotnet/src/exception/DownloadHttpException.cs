namespace k_spider_dotnet.exception;

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