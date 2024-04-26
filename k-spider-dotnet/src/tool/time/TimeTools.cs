using System;

namespace k_spider_dotnet.tool.time;

public static class TimeTools
{
    public const string DfTimeFormat = "yyyy-MM-dd HH:mm:ss";

    public static DateTime GetDateByTimeStr(string timeStr, string format)
    {
        return DateTime.ParseExact(timeStr, format, null);
    }
}