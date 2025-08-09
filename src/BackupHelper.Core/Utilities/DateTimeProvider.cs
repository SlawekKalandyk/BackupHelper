using BackupHelper.Abstractions;

namespace BackupHelper.Core.Utilities;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime Now => DateTime.Now;
}