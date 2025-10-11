namespace BackupHelper.Core.Utilities;

// https://pubs.opengroup.org/onlinepubs/7908799/xsh/strftime.html
public static class SimplifiedPOSIXDateTimeResolver
{
    public static string Resolve(string format, DateTime dateTime)
    {
        return format
            .Replace("%Y", dateTime.Year.ToString())
            .Replace("%m", dateTime.Month.ToString("D2"))
            .Replace("%d", dateTime.Day.ToString("D2"))
            .Replace("%H", dateTime.Hour.ToString("D2"))
            .Replace("%M", dateTime.Minute.ToString("D2"))
            .Replace("%S", dateTime.Second.ToString("D2"));
    }
}
