namespace BackupHelper.Sources.Abstractions;

public interface ISource : IDisposable
{
    const string PrefixSeparator = "://";

    /// <summary>
    /// Returns the scheme of the source, e.g., "smb" or "file".
    /// </summary>
    string GetScheme();

    /// <summary>
    /// Returns the scheme prefix for the source, e.g., "smb://" or "file://".
    /// </summary>
    string GetSchemePrefix() => GetScheme() + PrefixSeparator;

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
        string path,
        CancellationToken cancellationToken = default
    );

    Task<long> GetFileSizeAsync(string path, CancellationToken cancellationToken = default);
}
