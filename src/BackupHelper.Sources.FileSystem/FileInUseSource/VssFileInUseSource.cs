using System.IO.Compression;
using BackupHelper.Abstractions.ConnectionPooling;
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
    private readonly VssConnectionPool _connectionPool;

    public VssFileInUseSource(ILoggerFactory loggerFactory)
    {
        _connectionPool = new VssConnectionPool(loggerFactory.CreateLogger<VssConnectionPool>(), loggerFactory.CreateLogger<VssBackup>());
    }

    public Stream GetStream(string path)
    {
        var volume = Path.GetPathRoot(path)!;
        var vssBackup = _connectionPool.GetConnection(volume);

        try
        {
            var snapshotPath = vssBackup.GetSnapshotPath(path);

            return new PooledConnectionStream<VssBackup, string>(
                new FileStream(snapshotPath, FileMode.Open, FileAccess.Read, FileShare.Read),
                vssBackup,
                volume,
                _connectionPool);
        }
        catch
        {
            _connectionPool.ReturnConnection(volume, vssBackup);

            throw;
        }
    }

    public IEnumerable<string> GetSubDirectories(string path)
    {
        return ExecuteWithConnection(path, Directory.GetDirectories);
    }

    public IEnumerable<string> GetFiles(string path)
    {
        return ExecuteWithConnection(path, Directory.GetFiles);
    }

    private T ExecuteWithConnection<T>(string path, Func<string, T> operation)
    {
        var volume = Path.GetPathRoot(path)!;
        var vssBackup = _connectionPool.GetConnection(volume);

        try
        {
            var snapshotPath = vssBackup.GetSnapshotPath(path);
            var result = operation(snapshotPath);
            _connectionPool.ReturnConnection(volume, vssBackup);

            return result;
        }
        catch
        {
            _connectionPool.ReturnConnection(volume, vssBackup);

            throw;
        }
    }

    public void Dispose()
    {
        _connectionPool.Dispose();
    }
}