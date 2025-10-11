using BackupHelper.Abstractions;

namespace BackupHelper.Core.Tests.BackupZipping;

public class BackupPlanZipperTestsDateTimeProvider : IDateTimeProvider
{
    private DateTime _now;
    public DateTime Now
    {
        get => _now;
        set => _now = value;
    }
}
