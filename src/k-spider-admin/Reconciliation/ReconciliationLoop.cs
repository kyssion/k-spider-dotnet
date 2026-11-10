using KSpider.Admin.Common.Background;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KSpider.Admin.Reconciliation;

/// <summary>
///     周期触发对账扫描。
/// </summary>
/// <remarks>
///     循环本身不包含业务逻辑，只负责节拍与异常隔离；具体扫描、探测与重派在
///     <see cref="ReconciliationService" />。
/// </remarks>
/// <param name="reconciliation">对账领域服务。</param>
/// <param name="options">对账周期配置。</param>
/// <param name="logger">结构化日志。</param>
public sealed class ReconciliationLoop(
    IReconciliationService reconciliation,
    IOptions<ReconciliationOptions> options,
    ILogger<ReconciliationLoop> logger)
    : PeriodicBackgroundService(TimeSpan.FromSeconds(options.Value.TickSeconds), logger)
{
    /// <summary>
    ///     执行一轮对账。
    /// </summary>
    /// <param name="cancellationToken">宿主停止令牌。</param>
    protected override Task RunOnceAsync(CancellationToken cancellationToken)
    {
        return reconciliation.ReconcileStaleTasksAsync(cancellationToken);
    }
}
