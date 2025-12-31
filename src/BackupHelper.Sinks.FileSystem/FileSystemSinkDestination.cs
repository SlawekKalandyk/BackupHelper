using BackupHelper.Sinks.Abstractions;

namespace BackupHelper.Sinks.FileSystem;

public record FileSystemSinkDestination(string DestinationDirectory) : ISinkDestination
{
    public const string SinkName = "FileSystem";
    public string Name => SinkName;

    public ISink CreateSink() => new FileSystemSink(this);
}