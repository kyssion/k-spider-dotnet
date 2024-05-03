namespace k_spider_dotnet.tool.time;

public static class TimeTools
{
    public const string TimeFormatForStrikethrough = "yyyy-MM-dd HH:mm:ss";
    public const string TimeFormatForBackSlash = "yyyy/MM/dd HH:mm:ss";

    public static DateTime GetDateByTimeStrForFormat(string timeStr, string format)
    {
        return DateTime.ParseExact(timeStr, format, null);
    }
}