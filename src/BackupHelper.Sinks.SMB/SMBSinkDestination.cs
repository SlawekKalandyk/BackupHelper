using BackupHelper.Sinks.Abstractions;

namespace BackupHelper.Sinks.SMB;

public record SMBSinkDestination(string Server, string ShareName, string DestinationDirectory = "")
    : ISinkDestination
{
    public const string SinkKind = "SMB";
    public string Kind => SinkKind;
}
