using Microsoft.Extensions.Logging;

namespace KSpider.Common.Logger;

/// <summary>
///     统一日志工厂 : DI 之外的调用方 ( Spider 爬虫类 / Devtools 过渡期 ) 从这里拿 ILogger ,
///     静态类不能作泛型类型参数时用 GetLogger(Type) 重载
/// </summary>
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