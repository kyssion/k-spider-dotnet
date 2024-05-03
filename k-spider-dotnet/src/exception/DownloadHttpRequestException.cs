namespace k_spider_dotnet.exception;

public class DownloadHttpRequestException :DownloadHttpException
{
    public DownloadHttpRequestException(string newsUrl, string message) : base(newsUrl,message)
    {
    }

    public DownloadHttpRequestException(string newsUrl , string message, Exception innerException) : base(newsUrl,message, innerException)
    {

    }
}