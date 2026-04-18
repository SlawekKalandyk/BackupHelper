using System.Collections.Concurrent;

namespace BackupHelper.Core.FileZipping;

public abstract class FileZipperBase : IFileZipper
{
    protected readonly ConcurrentBag<string> _failedFiles = new();

    protected FileZipperBase(string zipFilePath, bool overwriteFileIfExists)
    {
        ZipFilePath = zipFilePath;
        OverwriteFileIfExists = overwriteFileIfExists;
    }

    public int ThreadLimit { get; set; } = 1;
    public int MemoryLimitMB { get; set; } = 0;
    public IReadOnlyCollection<string> FailedFiles => _failedFiles;
    public virtual int DefaultCompressionLevel => 9;
    public abstract bool HasToBeSaved { get; }
    protected string ZipFilePath { get; }
    protected bool OverwriteFileIfExists { get; }

    public abstract Task AddFileAsync(
        string filePath,
        string zipPath = "",
        int? compressionLevel = null,
        CancellationToken cancellationToken = default
    );

    public abstract Task AddDirectoryAsync(
        string directoryPath,
        string zipPath = "",
        int? compressionLevel = null,
        CancellationToken cancellationToken = default
    );

    public abstract Task AddDirectoryContentAsync(
        string directoryPath,
        string zipPath = "",
        int? compressionLevel = null,
        CancellationToken cancellationToken = default
    );

    public abstract Task SaveAsync(CancellationToken cancellationToken = default);

    public virtual Task WaitAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public virtual ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
