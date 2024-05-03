namespace k_spider_dotnet.exception;

public class DownloadHttpException:Exception
{
    public string NewsUrl { get; set; }

    public DownloadHttpException(string newsUrl, string message) : base(message)
    {
        this.NewsUrl = newsUrl;
    }

    public DownloadHttpException(string newsUrl , string message, Exception innerException) : base(message, innerException)
    {
        this.NewsUrl = newsUrl;

    }
}