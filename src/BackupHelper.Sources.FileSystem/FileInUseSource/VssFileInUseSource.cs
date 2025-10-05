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
        _vssBackupPool = new VssBackupPool(loggerFactory.CreateLogger<VssBackupPool>(), loggerFactory.CreateLogger<VssBackup>());
    }

    public Stream GetStream(string path)
    {
        var volume = Path.GetPathRoot(path)!;
        var vssBackup = _vssBackupPool.GetResource(volume);

        try
        {
            var snapshotPath = vssBackup.GetSnapshotPath(path);

            return new PooledResourceStream<VssBackup, string>(
                new FileStream(snapshotPath, FileMode.Open, FileAccess.Read, FileShare.Read),
                vssBackup,
                volume,
                _vssBackupPool);
        }
        catch
        {
            _vssBackupPool.ReturnResource(volume, vssBackup);

            throw;
        }
    }

    public IEnumerable<string> GetSubDirectories(string path)
    {
        return ExecuteWithVssBackup(path, Directory.GetDirectories);
    }

    public IEnumerable<string> GetFiles(string path)
    {
        return ExecuteWithVssBackup(path, Directory.GetFiles);
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