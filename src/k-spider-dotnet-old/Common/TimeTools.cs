namespace KSpider.Common;

public static class TimeTools
{
    /// <summary>
    ///     横杠日期格式 : 列表类接口时间 ( 东财列表 / 各快讯源等 )
    /// </summary>
    public const string TimeFormatForStrikethrough = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    ///     斜杠日期格式 : 详情类接口时间 ( 东财详情等 )
    /// </summary>
    public const string TimeFormatForBackSlash = "yyyy/MM/dd HH:mm:ss";

    /// <summary>
    ///     严格解析 : 格式不匹配直接抛 FormatException , 源改版最先在这里暴露
    /// </summary>
    public static DateTime GetDateByTimeStrForFormat(string timeStr, string format)
    {
        return DateTime.ParseExact(timeStr, format, null);
    }
    public static double GetDiffInSeconds(DateTime item1, DateTime item2)
    {
        return (item1 - item2).TotalSeconds;
    }
}
