using BackupHelper.Connectors.Azure;
using BackupHelper.Sinks.Abstractions;

namespace BackupHelper.Sinks.Azure;

public class AzureBlobStorageSink : SinkBase<AzureBlobStorageSinkDestination>
{
    private readonly AzureBlobStorage _storage;

    public AzureBlobStorageSink(
        AzureBlobStorageSinkDestination destination,
        AzureBlobCredential credential
    )
        : base(destination)
    {
        _storage = new AzureBlobStorage(credential);
    }

    public override string Description =>
        "Azure Blob Storage Sink to "
        + $"{TypedDestination.AccountName}, Container: {TypedDestination.Container}";

    public override async Task UploadAsync(
        string sourceFilePath,
        CancellationToken cancellationToken = default
    )
    {
        await using var fileStream = File.OpenRead(sourceFilePath);
        await _storage.UploadBlobAsync(
            TypedDestination.Container,
            Path.GetFileName(sourceFilePath),
            fileStream,
            cancellationToken
        );
    }

    public override async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _storage.IsContainerAvailableAsync(
                TypedDestination.Container,
                cancellationToken
            );
        }
        catch
        {
            return false;
        }
    }
}