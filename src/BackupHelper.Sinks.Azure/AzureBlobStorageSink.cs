using BackupHelper.Abstractions.Credentials;
using BackupHelper.Connectors.Azure;
using BackupHelper.Sinks.Abstractions;
using BackupHelper.Sinks.Abstractions.Retention;

namespace BackupHelper.Sinks.Azure;

public class AzureBlobStorageSink : SinkBase<AzureBlobStorageSinkDestination>, IPrunableSink
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

    public async Task PruneBackupsAsync(
        string uploadedBackupFileName,
        int maxBackups,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (maxBackups <= 0 || string.IsNullOrWhiteSpace(uploadedBackupFileName))
        {
            return;
        }

        var containerName = TypedDestination.Container;
        var manifest = await ReadManifestAsync(containerName, cancellationToken);
        var existingBlobNames = await _storage.GetBlobNamesAsync(containerName, cancellationToken);
        var pruningPlan = BackupsRetentionPlanner.CreatePlanForCaseSensitiveStorage(
            manifest,
            existingBlobNames,
            uploadedBackupFileName,
            maxBackups
        );

        foreach (var backupFileName in pruningPlan.BackupFileNamesToDelete)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _storage.DeleteBlobIfExistsAsync(
                containerName,
                backupFileName,
                cancellationToken
            );
        }

        await WriteManifestAsync(containerName, pruningPlan.UpdatedManifest, cancellationToken);
    }

    private async Task<BackupsManifest> ReadManifestAsync(
        string containerName,
        CancellationToken cancellationToken
    )
    {
        var manifestJson = await _storage.DownloadBlobTextAsync(
            containerName,
            BackupsRetentionConstants.ManifestFileName,
            cancellationToken
        );

        return BackupsManifestJsonSerializer.DeserializeOrDefault(manifestJson);
    }

    private async Task WriteManifestAsync(
        string containerName,
        BackupsManifest manifest,
        CancellationToken cancellationToken
    )
    {
        var manifestJson = BackupsManifestJsonSerializer.Serialize(manifest);
        await _storage.UploadBlobTextAsync(
            containerName,
            BackupsRetentionConstants.ManifestFileName,
            manifestJson,
            cancellationToken
        );
    }
}
