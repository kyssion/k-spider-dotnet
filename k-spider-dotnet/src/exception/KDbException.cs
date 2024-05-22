namespace k_spider_dotnet.exception;

public class KDbException : Exception
{
    public KDbException(string message) : base(message)
    {
    }

    public KDbException(string message, Exception innerException) : base(message, innerException)
    {
    }
}