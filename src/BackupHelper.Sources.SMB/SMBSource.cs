using BackupHelper.Abstractions;
using BackupHelper.Abstractions.ResourcePooling;
using BackupHelper.Sources.Abstractions;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Sources.SMB;

public class SMBSource : ISource
{
    private readonly SMBConnectionPool _connectionPool;

    public SMBSource(ICredentialsProvider credentialsProvider, ILoggerFactory loggerFactory)
    {
        _connectionPool = new SMBConnectionPool(credentialsProvider, loggerFactory.CreateLogger<SMBConnectionPool>());
    }

    public static string Scheme => "smb";

    public string GetScheme() => Scheme;

    public Stream GetStream(string path)
    {
        var shareInfo = SMBShareInfo.FromFilePath(path);
        var smbPath = SMBHelper.StripShareInfo(path);
        var connection = _connectionPool.GetResource(shareInfo);
        
        try
        {
            var stream = connection.GetStream(smbPath);
            return new PooledResourceStream<SMBConnection, SMBShareInfo>(stream, connection, shareInfo, _connectionPool);
        }
        catch
        {
            _connectionPool.ReturnResource(shareInfo, connection);
            throw;
        }
    }

    public IEnumerable<string> GetSubDirectories(string path)
    {
        return ExecuteWithConnection(path, (connection, smbPath, shareInfo) =>
            connection.GetSubDirectories(smbPath).Select(dir => Path.Join(shareInfo.ToString(), dir)).ToList());
    }

    public IEnumerable<string> GetFiles(string path)
    {
        return ExecuteWithConnection(path, (connection, smbPath, shareInfo) =>
            connection.GetFiles(smbPath).Select(file => Path.Join(shareInfo.ToString(), file)).ToList());
    }

    public bool FileExists(string path)
    {
        return ExecuteWithConnection(path, (connection, smbPath, _) =>
            connection.FileExists(smbPath));
    }

    public bool DirectoryExists(string path)
    {
        return ExecuteWithConnection(path, (connection, smbPath, _) =>
            connection.DirectoryExists(smbPath));
    }

    public DateTime? GetFileLastWriteTime(string path)
    {
        return ExecuteWithConnection(path, (connection, smbPath, _) =>
            connection.GetFileLastWriteTime(smbPath));
    }

    public DateTime? GetDirectoryLastWriteTime(string path)
    {
        return ExecuteWithConnection(path, (connection, smbPath, _) =>
            connection.GetDirectoryLastWriteTime(smbPath));
    }

    public long GetFileSize(string path)
    {
        return ExecuteWithConnection(path, (connection, smbPath, _) =>
            connection.GetFileSize(smbPath));
    }

    private T ExecuteWithConnection<T>(string path, Func<SMBConnection, string, SMBShareInfo, T> operation)
    {
        var shareInfo = SMBShareInfo.FromFilePath(path);
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

    public void Dispose()
    {
        _connectionPool.Dispose();
    }
}