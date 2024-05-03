namespace k_spider_dotnet.exception;

public class DbException : Exception
{
    public DbException(string message):base(message) { }
    public DbException(string message, Exception innerException) : base(message, innerException) { }
}