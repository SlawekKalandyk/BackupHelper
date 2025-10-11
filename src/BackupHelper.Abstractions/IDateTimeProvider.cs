namespace BackupHelper.Abstractions;

public interface IDateTimeProvider
{
    DateTime Now { get; }
}
