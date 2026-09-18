namespace SmartSolarMicrogrid.API.Utilities;

public static class ColomboTime
{
    public static readonly TimeZoneInfo Zone = Find("Asia/Colombo", "Sri Lanka Standard Time");

    public static DateTime ToLocal(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(utc, Zone);

    public static DateTime ToUtc(DateOnly day, TimeOnly time) =>
        TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(time), Zone);

    public static DateTime NowLocal() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

    private static TimeZoneInfo Find(string ianaId, string windowsId)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(ianaId); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById(windowsId); }
    }
}
