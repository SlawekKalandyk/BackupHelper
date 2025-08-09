using BackupHelper.Core.Utilities;

namespace BackupHelper.Core.Tests;

[TestFixture]
public class CronExpressionResolverTests
{
    [Test]
    public void GivenRegularTaskAndLocalDate_WhenGettingLastOccurrenceBeforeGivenDate_ThenReturnsCorrectLocalDate()
    {
        var cronExpression = "0 0 * * *"; // Every day at midnight
        var dateTime = new DateTime(2025, 6, 28, 0, 30, 0, DateTimeKind.Local);
        var timeZoneInfo = TimeZoneInfo.Local;

        var lastOccurrence = CronExpressionResolver.GetLastOccurrenceBeforeDateTime(cronExpression, dateTime, timeZoneInfo);
        var expectedDateTime = new DateTime(2025, 6, 28, 0, 0, 0, DateTimeKind.Local);

        Assert.That(lastOccurrence, Is.EqualTo(expectedDateTime));
    }

    [Test]
    public void GivenRegularTaskAndUtcDate_WhenGettingLastOccurrenceBeforeGivenDate_ThenReturnsCorrectUtcDate()
    {
        var cronExpression = "0 0 * * *"; // Every day at midnight
        var dateTime = new DateTime(2025, 6, 28, 0, 30, 0, DateTimeKind.Utc);
        var timeZoneInfo = TimeZoneInfo.Utc;

        var lastOccurrence = CronExpressionResolver.GetLastOccurrenceBeforeDateTime(cronExpression, dateTime, timeZoneInfo);
        var expectedDateTime = new DateTime(2025, 6, 28, 0, 0, 0, DateTimeKind.Utc);

        Assert.That(lastOccurrence, Is.EqualTo(expectedDateTime));
    }
}