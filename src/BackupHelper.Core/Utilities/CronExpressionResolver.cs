using Cronos;

namespace BackupHelper.Core.Utilities;

public static class CronExpressionResolver
{
    public static DateTime GetLastOccurrenceBeforeDateTime(
        string cronExpression,
        DateTime dateTime,
        TimeZoneInfo timeZoneInfo
    )
    {
        var utcDateTime = ToUtcTime(dateTime);

        var expression = CronExpression.Parse(cronExpression);
        var nextOccurrence =
            expression.GetNextOccurrence(utcDateTime, timeZoneInfo) ?? DateTime.MaxValue;
        var secondNextOccurrence =
            expression.GetNextOccurrence(nextOccurrence, timeZoneInfo) ?? DateTime.MaxValue;

        // TODO: handle both at max value

        var timePeriod = (secondNextOccurrence - nextOccurrence) * 1.5;
        var attempt = 1;

        List<DateTime> occurrences;

        do
        {
            var startDate = utcDateTime - timePeriod * attempt;
            occurrences = expression.GetOccurrences(startDate, utcDateTime, timeZoneInfo).ToList();
            attempt++;
        } while (occurrences.Count == 0);

        var previousOccurence = occurrences.Last();

        return ToLocalTime(previousOccurence, timeZoneInfo);
    }

    private static DateTime ToUtcTime(DateTime dateTime)
    {
        return dateTime.Kind == DateTimeKind.Utc
            ? dateTime
            : TimeZoneInfo.ConvertTimeToUtc(dateTime);
    }

    private static DateTime ToLocalTime(DateTime dateTime, TimeZoneInfo timeZoneInfo)
    {
        return dateTime.Kind == DateTimeKind.Utc
            ? TimeZoneInfo.ConvertTimeFromUtc(dateTime, timeZoneInfo)
            : dateTime;
    }
}
