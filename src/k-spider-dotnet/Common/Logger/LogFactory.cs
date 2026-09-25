using Microsoft.Extensions.Logging;

namespace KSpider.Common.Logger;

public class LogFactory
{
    private static readonly ILoggerFactory Factory = LoggerFactory.Create(builder => builder.AddSimpleConsole(
        options =>
        {
            options.IncludeScopes = true;
            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss";
        })
    );

    public static ILogger<T> GetLogger<T>()
    {
        return Factory.CreateLogger<T>();
    }

    /// <summary>
    ///     静态类不能作泛型类型参数 , 这类调用方 ( 如 VerifiedHttp ) 用本重载传 typeof
    /// </summary>
    public static ILogger GetLogger(Type type)
    {
        return Factory.CreateLogger(type);
    }
}