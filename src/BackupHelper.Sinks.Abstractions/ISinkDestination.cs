namespace BackupHelper.Sinks.Abstractions;

public interface ISinkDestination
{
    string Name { get; }
    ISink CreateSink();
}