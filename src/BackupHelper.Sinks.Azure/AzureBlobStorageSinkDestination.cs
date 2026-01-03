using BackupHelper.Sinks.Abstractions;

namespace BackupHelper.Sinks.Azure;

public class AzureBlobStorageSinkDestination : ISinkDestination
{
    public const string SinkName = "AzureBlobStorage";
    public string Name => SinkName;

    public ISink CreateSink() => new AzureBlobStorageSink(this);
}