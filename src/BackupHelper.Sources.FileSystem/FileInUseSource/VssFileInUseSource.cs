using BackupHelper.Abstractions.ResourcePooling;
using BackupHelper.Sources.FileSystem.Vss;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Sources.FileSystem.FileInUseSource;

public class VssFileInUseSourceFactory : IFileInUseSourceFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public VssFileInUseSourceFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public IFileInUseSource Create()
    {
        return new VssFileInUseSource(_loggerFactory);
    }
}

public class VssFileInUseSource : IFileInUseSource
{
    private readonly VssBackupPool _vssBackupPool;

    public VssFileInUseSource(ILoggerFactory loggerFactory)
    {
        _vssBackupPool = new VssBackupPool(
            loggerFactory.CreateLogger<VssBackupPool>(),
            loggerFactory.CreateLogger<VssBackup>()
        );
    }

    public Task<Stream> GetStreamAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var volume = Path.GetPathRoot(path)!;
        var vssBackup = _vssBackupPool.GetResource(volume);

        try
        {
            var snapshotPath = vssBackup.GetSnapshotPath(path);

            return Task.FromResult<Stream>(
                new PooledResourceStream<VssBackup, string>(
                new FileStream(
                    snapshotPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    81920,
                    FileOptions.Asynchronous | FileOptions.SequentialScan
                ),
                vssBackup,
                volume,
                _vssBackupPool
                )
            );
        }
        catch
        {
            _vssBackupPool.ReturnResource(volume, vssBackup);

            throw;
        }
    }

    public Task<IEnumerable<string>> GetSubDirectoriesAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IEnumerable<string>>(
            ExecuteWithVssBackup(path, Directory.GetDirectories)
        );
    }

    public Task<IEnumerable<string>> GetFilesAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IEnumerable<string>>(ExecuteWithVssBackup(path, Directory.GetFiles));
    }

    private T ExecuteWithVssBackup<T>(string path, Func<string, T> operation)
    {
        var volume = Path.GetPathRoot(path)!;
        var vssBackup = _vssBackupPool.GetResource(volume);

        try
        {
            var snapshotPath = vssBackup.GetSnapshotPath(path);
            var result = operation(snapshotPath);
            _vssBackupPool.ReturnResource(volume, vssBackup);

            return result;
        }
        catch
        {
            _vssBackupPool.ReturnResource(volume, vssBackup);

            throw;
        }
    }

    public void Dispose()
    {
        _vssBackupPool.Dispose();
    }
}
