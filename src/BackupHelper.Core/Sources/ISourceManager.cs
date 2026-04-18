namespace BackupHelper.Core.Sources;

public interface ISourceManager
{
    Task<Stream> GetStreamAsync(string path, CancellationToken cancellationToken = default);

    Task<IEnumerable<string>> GetSubDirectoriesAsync(
        string path,
        CancellationToken cancellationToken = default
    );

    Task<IEnumerable<string>> GetFilesAsync(string path, CancellationToken cancellationToken = default);

    Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default);

    Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default);

    Task<DateTime?> GetFileLastWriteTimeAsync(
        string path,
        CancellationToken cancellationToken = default
    );

    Task<DateTime?> GetDirectoryLastWriteTimeAsync(
        string directoryPath,
        CancellationToken cancellationToken = default
    );

    Task<long> GetFileSizeAsync(string path, CancellationToken cancellationToken = default);
}
