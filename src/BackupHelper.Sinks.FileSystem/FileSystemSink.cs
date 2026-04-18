using BackupHelper.Sinks.Abstractions;
using BackupHelper.Sinks.Abstractions.Retention;

namespace BackupHelper.Sinks.FileSystem;

public class FileSystemSink : SinkBase<FileSystemSinkDestination>, IPrunableSink
{
    public FileSystemSink(FileSystemSinkDestination destination)
        : base(destination) { }

    public override string Description =>
        $"File System Sink to {TypedDestination.DestinationDirectory}";

    public override async Task UploadAsync(
        string sourceFilePath,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(TypedDestination.DestinationDirectory);
        var destinationPath = Path.Join(
            TypedDestination.DestinationDirectory,
            Path.GetFileName(sourceFilePath)
        );

        await using var sourceStream = new FileStream(
            sourceFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan
        );

        await using var destinationStream = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan
        );

        await sourceStream.CopyToAsync(destinationStream, cancellationToken);
    }

    public override Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(TypedDestination.DestinationDirectory))
        {
            return Task.FromResult(false);
        }

        try
        {
            Directory.CreateDirectory(TypedDestination.DestinationDirectory);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
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

        Directory.CreateDirectory(TypedDestination.DestinationDirectory);

        var manifestPath = Path.Join(
            TypedDestination.DestinationDirectory,
            BackupsRetentionConstants.ManifestFileName
        );
        var manifest = await ReadManifestAsync(manifestPath, cancellationToken);
        var existingBackupFileNames = Directory
            .GetFiles(TypedDestination.DestinationDirectory)
            .Select(Path.GetFileName)
            .Where(fileName => !string.IsNullOrWhiteSpace(fileName))
            .Select(fileName => fileName!);
        var pruningPlan = BackupsRetentionPlanner.CreatePlanForCaseInsensitiveStorage(
            manifest,
            existingBackupFileNames,
            uploadedBackupFileName,
            maxBackups
        );

        foreach (var backupFileName in pruningPlan.BackupFileNamesToDelete)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var filePath = Path.Join(TypedDestination.DestinationDirectory, backupFileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        await WriteManifestAsync(manifestPath, pruningPlan.UpdatedManifest, cancellationToken);
    }

    private static async Task<BackupsManifest> ReadManifestAsync(
        string manifestPath,
        CancellationToken cancellationToken
    )
    {
        if (!File.Exists(manifestPath))
        {
            return new BackupsManifest();
        }

        var manifestJson = await File.ReadAllTextAsync(manifestPath, cancellationToken);
        return BackupsManifestJsonSerializer.DeserializeOrDefault(manifestJson);
    }

    private static async Task WriteManifestAsync(
        string manifestPath,
        BackupsManifest manifest,
        CancellationToken cancellationToken
    )
    {
        var tempManifestPath = manifestPath + ".tmp";
        var manifestJson = BackupsManifestJsonSerializer.Serialize(manifest);

        await File.WriteAllTextAsync(tempManifestPath, manifestJson, cancellationToken);
        File.Move(tempManifestPath, manifestPath, overwrite: true);

        if (File.Exists(tempManifestPath))
        {
            File.Delete(tempManifestPath);
        }
    }
}
