using KSpider.Spider.Verify.Model;

namespace KSpider.Spider.Verify;

/// <summary>
///     通过策略 : 拿到识别结果后想办法把这次验证过掉 , 成功后交出可复用的会话。
///     新增手段 = 新增一个实现 + 在 <see cref="VerificationRegistry" /> 注册一行 ;
///     策略只声明自己能处理哪些 <see cref="VerificationKind" /> , 不认识的一律不接。
///     实现必须线程安全 : 多个源的任务并行跑 , 同一个策略实例会被并发调用。
/// </summary>
public interface IVerificationSolver
{
    /// <summary>策略名 , 会写进日志</summary>
    string Name { get; }

    /// <summary>
    ///     处理代价 , 越小越先试 ( 会话复用 0 &lt; 浏览器过挑战 10 &lt; 滑块 20 &lt; 人工升级 99 )。
    ///     管线按代价升序逐个尝试 , 先便宜后昂贵。
    /// </summary>
    int Cost { get; }

    /// <summary>本策略能处理的验证类型</summary>
    IReadOnlyCollection<VerificationKind> Kinds { get; }

    Task<VerificationSolveResult> SolveAsync(VerificationSolveRequest request, CancellationToken cancellationToken);
}
