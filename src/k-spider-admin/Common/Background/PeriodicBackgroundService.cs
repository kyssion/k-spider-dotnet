using KSpider.Admin.Common.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KSpider.Admin.Common.Background;

/// <summary>
///     周期循环基类：单轮失败只记录日志，不终止整个 host；测试可直接调用 <see cref="RunOnceAsync" />。
/// </summary>
public abstract class PeriodicBackgroundService : BackgroundService
{
    private readonly TimeSpan _period;
    private readonly ILogger _logger;

    /// <summary>
    ///     创建周期循环。
    /// </summary>
    /// <param name="period">循环周期，必须大于零。</param>
    /// <param name="logger">单轮失败日志。</param>
    protected PeriodicBackgroundService(TimeSpan period, ILogger logger)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(period, TimeSpan.Zero);

        _period = period;
        _logger = logger;
    }

    /// <summary>
    ///     执行一轮业务；异常由基类记录并延迟到下一轮。
    /// </summary>
    /// <param name="cancellationToken">宿主停止令牌。</param>
    protected abstract Task RunOnceAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     宿主启动后按周期执行；单轮异常在基类内记录，不终止 host。
    /// </summary>
    /// <param name="stoppingToken">宿主停止令牌。</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_period);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                AdminLog.LoopFailed(_logger, exception, GetType().Name);
            }
        }
    }
}
