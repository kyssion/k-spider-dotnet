namespace KSpider.Admin.Tasks;

/// <summary>
///     任务进入终态后的观察者。
/// </summary>
/// <remarks>
///     调度器用它释放同渠道互斥；其他模块可用于指标或通知。观察者实现必须快速、幂等，
///     且不能依赖调用方请求的取消令牌（终态释放通常使用独立令牌）。
/// </remarks>
public interface ITaskTerminalObserver
{
    /// <summary>
    ///     处理任务终态。
    /// </summary>
    /// <param name="taskId">任务主键。</param>
    /// <param name="channelId">任务所属渠道。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    ValueTask OnTaskTerminalAsync(long taskId, long channelId, CancellationToken cancellationToken);
}
