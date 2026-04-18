using BackupHelper.Sinks.Abstractions;

namespace BackupHelper.Sinks.FileSystem;

public class FileSystemSink : SinkBase<FileSystemSinkDestination>
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
        return Task.FromResult(Directory.Exists(TypedDestination.DestinationDirectory));
    }
}