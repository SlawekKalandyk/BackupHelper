using BackupHelper.Sinks.Abstractions;

namespace BackupHelper.Sinks.Azure;

internal class AzureBlobStorageSink : SinkBase<AzureBlobStorageSinkDestination>
{
    public AzureBlobStorageSink(AzureBlobStorageSinkDestination destination)
        : base(destination) { }

    public override string Description { get; }

    public override Task UploadAsync(
        string sourceFilePath,
        CancellationToken cancellationToken = default
    ) => throw new NotImplementedException();

    public override Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}