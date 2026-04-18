namespace BackupHelper.Sources.FileSystem.FileInUseSource;

public interface IFileInUseSourceFactory
{
    IFileInUseSource Create();
}

public interface IFileInUseSource : IDisposable
{
    Task<Stream> GetStreamAsync(string path, CancellationToken cancellationToken = default);

    Task<IEnumerable<string>> GetSubDirectoriesAsync(
        string path,
        CancellationToken cancellationToken = default
    );

    Task<IEnumerable<string>> GetFilesAsync(string path, CancellationToken cancellationToken = default);
}
