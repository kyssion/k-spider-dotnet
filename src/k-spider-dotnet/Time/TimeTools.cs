namespace KSpider.Time;

public static class TimeTools
{
    public const string TimeFormatForStrikethrough = "yyyy-MM-dd HH:mm:ss";
    public const string TimeFormatForBackSlash = "yyyy/MM/dd HH:mm:ss";

    public static DateTime GetDateByTimeStrForFormat(string timeStr, string format)
    {
        return DateTime.ParseExact(timeStr, format, null);
    }
    public static double GetDiffInSeconds(DateTime item1, DateTime item2)
    {
        return (item1 - item2).TotalSeconds;
    }
}