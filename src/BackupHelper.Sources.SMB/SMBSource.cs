using BackupHelper.Abstractions.Credentials;
using BackupHelper.Abstractions.ResourcePooling;
using BackupHelper.Connectors.SMB;
using BackupHelper.Connectors.SMB.IO;
using BackupHelper.Sources.Abstractions;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Sources.SMB;

public class SMBSource : ISource
{
    private readonly SMBConnectionPool _connectionPool;

    public SMBSource(ICredentialsProvider credentialsProvider, ILoggerFactory loggerFactory)
    {
        _connectionPool = new SMBConnectionPool(
            credentialsProvider,
            loggerFactory.CreateLogger<SMBConnectionPool>()
        );
    }

    public static string Scheme => "smb";

    public string GetScheme() => Scheme;

    public Task<Stream> GetStreamAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var shareInfo = SMBShareInfo.FromSMBPath(path);
        var smbPath = SMBHelper.StripShareInfo(path);
        var connection = _connectionPool.GetResource(shareInfo);

        try
        {
            var stream = connection.GetStream(smbPath);
            return Task.FromResult<Stream>(
                new PooledResourceStream<SMBConnection, SMBShareInfo>(
                    stream,
                    connection,
                    shareInfo,
                    _connectionPool
                )
            );
        }
        catch
        {
            _connectionPool.ReturnResource(shareInfo, connection);
            throw;
        }
    }

    public Task<IEnumerable<string>> GetSubDirectoriesAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        return ExecuteWithConnectionAsync<IEnumerable<string>>(
            path,
            (connection, smbPath, shareInfo) =>
                connection
                    .GetSubDirectories(smbPath)
                    .Select(dir => Path.Join(shareInfo.ToString(), dir))
                    .ToList(),
            cancellationToken
        );
    }

    public Task<IEnumerable<string>> GetFilesAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        return ExecuteWithConnectionAsync<IEnumerable<string>>(
            path,
            (connection, smbPath, shareInfo) =>
                connection
                    .GetFiles(smbPath)
                    .Select(file => Path.Join(shareInfo.ToString(), file))
                    .ToList(),
            cancellationToken
        );
    }

    public Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        return ExecuteWithConnectionAsync(
            path,
            (connection, smbPath, _) => connection.FileExists(smbPath),
            cancellationToken
        );
    }

    public Task<bool> DirectoryExistsAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        return ExecuteWithConnectionAsync(
            path,
            (connection, smbPath, _) => connection.DirectoryExists(smbPath),
            cancellationToken
        );
    }

    public Task<DateTime?> GetFileLastWriteTimeAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        return ExecuteWithConnectionAsync(
            path,
            (connection, smbPath, _) => connection.GetFileLastWriteTime(smbPath),
            cancellationToken
        );
    }

    public Task<DateTime?> GetDirectoryLastWriteTimeAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        return ExecuteWithConnectionAsync(
            path,
            (connection, smbPath, _) => connection.GetDirectoryLastWriteTime(smbPath),
            cancellationToken
        );
    }

    public Task<long> GetFileSizeAsync(string path, CancellationToken cancellationToken = default)
    {
        return ExecuteWithConnectionAsync(
            path,
            (connection, smbPath, _) => connection.GetFileSize(smbPath),
            cancellationToken
        );
    }

    private T ExecuteWithConnection<T>(
        string path,
        Func<SMBConnection, string, SMBShareInfo, T> operation
    )
    {
        var shareInfo = SMBShareInfo.FromSMBPath(path);
        var smbPath = SMBHelper.StripShareInfo(path);
        var connection = _connectionPool.GetResource(shareInfo);

        try
        {
            var result = operation(connection, smbPath, shareInfo);
            _connectionPool.ReturnResource(shareInfo, connection);
            return result;
        }
        catch
        {
            _connectionPool.ReturnResource(shareInfo, connection);
            throw;
        }
    }

    private Task<T> ExecuteWithConnectionAsync<T>(
        string path,
        Func<SMBConnection, string, SMBShareInfo, T> operation,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ExecuteWithConnection(path, operation));
    }

    public void Dispose()
    {
        _connectionPool.Dispose();
    }
}