using BackupHelper.Sinks.Abstractions;

namespace BackupHelper.Sinks.FileSystem;

public record FileSystemSinkDestination(string DestinationDirectory, int? MaxBackups = null)
    : ISinkDestination
{
    public const string SinkKind = "FileSystem";
    public string Kind => SinkKind;
}
