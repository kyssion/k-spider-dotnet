using KSpider.Spider.Verify;
using KSpider.Spider.Verify.Model;

namespace KSpider.Exceptions;

/// <summary>
///     请求被反爬验证拦住且自动通过失败。与 <see cref="DownloadHttpException" /> 族其它成员一样
///     按"这条数据失败"处理 ( 消耗一次 fail_count ) , 但带上验证类型 ,
///     便于日志与 NewsCheckJob 直接定位是哪一类验证。
/// </summary>
public class VerificationRequiredException : DownloadHttpException
{
    public VerificationRequiredException(string newsUrl, VerificationKind kind, string message)
        : base(newsUrl, message)
    {
        Kind = kind;
    }

    public VerificationKind Kind { get; }
}
