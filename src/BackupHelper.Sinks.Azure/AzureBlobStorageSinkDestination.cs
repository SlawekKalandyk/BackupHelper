using BackupHelper.Sinks.Abstractions;

namespace BackupHelper.Sinks.Azure;

public record AzureBlobStorageSinkDestination(
    string AccountName,
    string Container,
    int? MaxBackups = null
)
    : ISinkDestination
{
    public const string SinkKind = "AzureBlobStorage";
    public string Kind => SinkKind;
}