using BackupHelper.Sinks.Abstractions;

namespace BackupHelper.Sinks.FileSystem;

public class FileSystemSink : SinkBase<FileSystemSinkDestination>
{
    public FileSystemSink(FileSystemSinkDestination destination)
        : base(destination) { }

    public override string Description =>
        $"File System Sink to {TypedDestination.DestinationDirectory}";

    public override Task UploadAsync(
        string sourceFilePath,
        CancellationToken cancellationToken = default
    )
    {
        File.Copy(
            sourceFilePath,
            Path.Join(TypedDestination.DestinationDirectory, Path.GetFileName(sourceFilePath)),
            overwrite: true
        );
        return Task.CompletedTask;
    }

    public override Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Directory.Exists(TypedDestination.DestinationDirectory));
    }
}