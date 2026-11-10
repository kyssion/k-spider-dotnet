using KSpider.Admin.Common.Logging;
using KSpider.Admin.Repository.Abstractions;
using Microsoft.Extensions.Logging;

namespace KSpider.Admin.Scheduler;

/// <summary>
///     渠道启停命令结果。
/// </summary>
public enum ChannelCommandOutcome
{
    /// <summary>命令已生效，或目标状态本来就是该状态。</summary>
    Success,

    /// <summary>渠道不存在。</summary>
    NotFound,

    /// <summary>渠道已退役等不可变更状态。</summary>
    Conflict,
}

/// <summary>
///     渠道暂停/恢复命令。
/// </summary>
/// <remarks>
///     当前只修改 channel.status；在跑任务是否取消留给任务控制接口，避免一次命令同时改变
///     渠道配置与任务状态。暂停后调度循环不再创建新任务，恢复后下一轮到期的任务重新派发。
/// </remarks>
/// <param name="channels">渠道仓储。</param>
/// <param name="logger">结构化日志。</param>
public sealed class ChannelCommandService(
    IChannelRepository channels,
    ILogger<ChannelCommandService> logger)
{
    /// <summary>
    ///     暂停渠道。
    /// </summary>
    /// <param name="channelId">渠道主键。</param>
    /// <param name="operatorId">操作账号主键，用于审计日志。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>命令结果；已暂停时幂等返回 Success。</returns>
    public async Task<ChannelCommandOutcome> PauseAsync(
        long channelId,
        long operatorId,
        CancellationToken cancellationToken)
    {
        var channel = await channels.GetAsync(channelId, cancellationToken);
        if (channel is null)
        {
            return ChannelCommandOutcome.NotFound;
        }

        if (string.Equals(channel.Status, "retired", StringComparison.Ordinal))
        {
            return ChannelCommandOutcome.Conflict;
        }

        if (string.Equals(channel.Status, "paused", StringComparison.Ordinal))
        {
            return ChannelCommandOutcome.Success;
        }

        await channels.UpdateStatusAsync(channelId, "paused", cancellationToken);
        AdminLog.ChannelStatusChanged(logger, channelId, "paused", operatorId);
        return ChannelCommandOutcome.Success;
    }

    /// <summary>
    ///     恢复渠道。
    /// </summary>
    /// <param name="channelId">渠道主键。</param>
    /// <param name="operatorId">操作账号主键，用于审计日志。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>命令结果；已 active 时幂等返回 Success。</returns>
    public async Task<ChannelCommandOutcome> ResumeAsync(
        long channelId,
        long operatorId,
        CancellationToken cancellationToken)
    {
        var channel = await channels.GetAsync(channelId, cancellationToken);
        if (channel is null)
        {
            return ChannelCommandOutcome.NotFound;
        }

        if (string.Equals(channel.Status, "retired", StringComparison.Ordinal))
        {
            return ChannelCommandOutcome.Conflict;
        }

        if (string.Equals(channel.Status, "active", StringComparison.Ordinal))
        {
            return ChannelCommandOutcome.Success;
        }

        await channels.UpdateStatusAsync(channelId, "active", cancellationToken);
        AdminLog.ChannelStatusChanged(logger, channelId, "active", operatorId);
        return ChannelCommandOutcome.Success;
    }
}
