using KSpider.Common.Logger;
using KSpider.Spider.Verify.Model;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.Verify.Solver;

/// <summary>
///     人工升级策略 : 自动手段都用尽后的兜底 , 代价最高所以排最后。
///     它的价值在于**把问题说出来** —— 被反爬拦住时最糟的结果不是失败 ,
///     而是悄悄返回空数据、看起来一切正常 ( 与 NewsCheckJob 防"静默失效"是同一个出发点 ) 。
/// </summary>
public sealed class ManualEscalationSolver : IVerificationSolver
{
    private static readonly ILogger Log = LogFactory.GetLogger<ManualEscalationSolver>();
    /// <summary>全部验证类型快照 ( 兜底策略不挑类型 , 什么拦住都能升级告警 )</summary>
    private static readonly VerificationKind[] AllKinds = Enum.GetValues<VerificationKind>();

    /// <summary>策略名 , 进日志</summary>
    public string Name => "ManualEscalationSolver";

    /// <summary>代价最高 ( 99 ) : 永远排在最后 , 只在自动手段全部失败后兜底</summary>
    public int Cost => 99;

    /// <summary>兜底策略对全部验证类型生效</summary>
    public IReadOnlyCollection<VerificationKind> Kinds => AllKinds;

    /// <summary>
    ///     不做任何"通过"尝试 : 记 Error 级日志 ( 带验证形态与处置建议 ) ,
    ///     返回失败并标记需人工介入 , 由 NewsCheckJob 汇总告警。
    /// </summary>
    public Task<VerificationSolveResult> SolveAsync(VerificationSolveRequest request,
        CancellationToken cancellationToken)
    {
        Log.LogError(
            "[ManualEscalationSolver] 源被验证拦住且无可用自动手段 , 需人工介入 ; host : {Host} , kind : {Kind} , status : {Status} , url : {Url} , 依据 : {Evidence} , 建议 : {Advice}",
            request.Host, request.Challenge.Kind, request.Challenge.StatusCode, request.Url,
            request.Challenge.Evidence, AdviceFor(request.Challenge.Kind));
        return Task.FromResult(VerificationSolveResult.Failed(Name,
            $"{request.Challenge.Kind} 需人工介入", true));
    }

    /// <summary>按验证类型给出人工处置建议 ( 写进日志 )</summary>
    private static string AdviceFor(VerificationKind kind)
    {
        return kind switch
        {
            VerificationKind.RateLimited =>
                "限流只能退避 : 调大该源的抓取间隔或单轮页数 , 冷却期内不要重试",
            VerificationKind.SliderCaptcha =>
                "滑块默认不自动处理 ; 确认有权抓取该站后 , 在 VerificationRegistry 按源放开 SliderCaptcha",
            VerificationKind.ImageCaptcha =>
                "图形验证码需识图能力 ; 人工过一次验证后把 cookie 配进会话 , 或改走有授权的接口",
            VerificationKind.SmsCaptcha =>
                "短信验证码必须人工过验证 , 无法自动化 ; 检查是否误抓了需要登录的接口",
            VerificationKind.RiskControl =>
                "载荷级风控 : 核对请求头 / 签名参数是否随源站前端版本变化 ( 见 AGENTS 已知坑 )",
            VerificationKind.AccessDenied or VerificationKind.ServerGate =>
                "先确认是不是整站故障或 IP 被拉黑 , 再考虑是否需要更换出口 IP",
            _ => "人工打开该 URL 确认验证形态 , 再按形态补充识别器或通过策略"
        };
    }
}
