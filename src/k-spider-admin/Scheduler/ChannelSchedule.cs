using System.Text.Json;

namespace KSpider.Admin.Scheduler;

/// <summary>
///     channel.schedule 的 admin 侧最小实现：只支持 enabled + interval_seconds。
/// </summary>
/// <remarks>
///     cron / 时区 / 错过轮次语义仍是待确认项；当前对未知键直接失败，避免静默忽略配置。
///     解析失败只影响单个渠道，由调度循环记录后跳过。
/// </remarks>
/// <param name="Enabled">是否启用调度。</param>
/// <param name="IntervalSeconds">两轮之间的最小间隔（秒）。</param>
public sealed record ChannelSchedule(bool Enabled, int IntervalSeconds)
{
    /// <summary>
    ///     解析 channel.schedule JSON。
    /// </summary>
    /// <param name="json">数据库中的 schedule JSON 文本。</param>
    /// <returns>解析后的调度配置。</returns>
    /// <exception cref="FormatException">JSON 不是对象、键未知或取值不合法。</exception>
    /// <exception cref="JsonException">JSON 语法错误。</exception>
    public static ChannelSchedule Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException("channel.schedule 必须是 JSON 对象");
        }

        var enabled = false;
        var intervalSeconds = 0;

        foreach (var property in document.RootElement.EnumerateObject())
        {
            switch (property.Name)
            {
                case "enabled":
                    if (property.Value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    {
                        throw new FormatException("channel.schedule.enabled 必须是布尔值");
                    }

                    enabled = property.Value.GetBoolean();
                    break;
                case "interval_seconds":
                    if (property.Value.ValueKind != JsonValueKind.Number ||
                        !property.Value.TryGetInt32(out intervalSeconds) ||
                        intervalSeconds <= 0)
                    {
                        throw new FormatException("channel.schedule.interval_seconds 必须是正整数");
                    }

                    break;
                default:
                    throw new FormatException($"channel.schedule 存在未知键：{property.Name}");
            }
        }

        if (enabled && intervalSeconds <= 0)
        {
            throw new FormatException("启用调度时必须提供 interval_seconds");
        }

        return new ChannelSchedule(enabled, intervalSeconds);
    }
}
