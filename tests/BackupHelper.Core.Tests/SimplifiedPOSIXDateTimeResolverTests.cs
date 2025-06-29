using BackupHelper.Core.Utilities;

namespace BackupHelper.Core.Tests;

[TestFixture]
public class SimplifiedPOSIXDateTimeResolverTests
{
    [Test]
    public void GivenNamingSchema_WhenResolvingWithGivenDate_ThenDateFormatPlaceholdersAreResolvedWithGivenDateValue()
    {
        var snapshotNamingSchema = "auto-%Y-%m-%d_%H-%M-%S";
        var dateTime = new DateTime(2025, 6, 28, 0, 30, 0);
        var actualResult = SimplifiedPOSIXDateTimeResolver.Resolve(snapshotNamingSchema, dateTime);
        var expectedResult = "auto-2025-06-28_00-30-00";
        Assert.That(actualResult, Is.EqualTo(expectedResult));
    }
}