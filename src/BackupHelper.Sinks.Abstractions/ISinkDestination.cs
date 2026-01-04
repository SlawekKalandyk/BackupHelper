namespace BackupHelper.Sinks.Abstractions;

public interface ISinkDestination
{
    string Kind { get; }
}