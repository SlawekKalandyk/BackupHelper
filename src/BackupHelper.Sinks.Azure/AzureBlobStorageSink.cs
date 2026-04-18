using BackupHelper.Abstractions.Credentials;
using BackupHelper.Connectors.Azure;
using BackupHelper.Sinks.Abstractions;

namespace BackupHelper.Sinks.Azure;

public class AzureBlobStorageSink : SinkBase<AzureBlobStorageSinkDestination>
{
    private readonly AzureBlobStorage _storage;

    public AzureBlobStorageSink(
        AzureBlobStorageSinkDestination destination,
        string accountName,
        SensitiveString sharedAccessSignature
    )
        : base(destination)
    {
        _storage = new AzureBlobStorage(accountName, sharedAccessSignature.Expose());
    }

    public override string Description =>
        "Azure Blob Storage Sink to "
        + $"{TypedDestination.AccountName}, Container: {TypedDestination.Container}";

    public override async Task UploadAsync(
        string sourceFilePath,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await using var fileStream = new FileStream(
            sourceFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan
        );

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