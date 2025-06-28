using BackupHelper.Abstractions;
using BackupHelper.Sources.Abstractions;

namespace BackupHelper.Sources.SMB;

public class SMBSource : ISource
{
    private readonly ICredentialsProvider _credentialsProvider;
    private readonly IDictionary<SMBShareInfo, SMBConnection> _connections = new Dictionary<SMBShareInfo, SMBConnection>();

    public static string Scheme => "smb";

    public string GetScheme() => Scheme;

    public SMBSource(ICredentialsProvider credentialsProvider)
    {
        _credentialsProvider = credentialsProvider;
    }

    public Stream GetStream(string path)
    {
        var shareInfo = SMBShareInfo.FromFilePath(path);
        var connection = GetConnection(shareInfo, path);
        var smbPath = SMBHelper.StripShareInfo(path);
        return connection.GetStream(smbPath);
    }

    public IEnumerable<string> GetSubDirectories(string path)
    {
        var shareInfo = SMBShareInfo.FromFilePath(path);
        var connection = GetConnection(shareInfo, path);
        var smbPath = SMBHelper.StripShareInfo(path);
        return connection.GetSubDirectories(smbPath).Select(dir => Path.Join(shareInfo.ToString(), dir));
    }

    public IEnumerable<string> GetFiles(string path)
    {
        var shareInfo = SMBShareInfo.FromFilePath(path);
        var connection = GetConnection(shareInfo, path);
        var smbPath = SMBHelper.StripShareInfo(path);
        return connection.GetFiles(smbPath).Select(file => Path.Join(shareInfo.ToString(), file));
    }

    private SMBConnection GetConnection(SMBShareInfo shareInfo, string path)
    {
        if (_connections.TryGetValue(shareInfo, out var connection))
        {
            if (connection.IsConnected)
                return connection;

            connection.Dispose();
            _connections.Remove(shareInfo);
            return GetConnection(shareInfo, path);
        }

        var credential = GetCredential(shareInfo);
        var smbConnection = new SMBConnection(
            shareInfo.ServerIPAddress,
            string.Empty,
            shareInfo.ShareName,
            credential.Username,
            credential.Password
        );
        _connections[shareInfo] = smbConnection;
        return smbConnection;
    }

    private SMBCredential GetCredential(SMBShareInfo shareInfo)
    {
        var credentialName = $@"\\{shareInfo.ServerIPAddress.ToString()}\{shareInfo.ShareName}";
        var (username, password) = _credentialsProvider.GetCredential(credentialName);

        return new SMBCredential(username, password);
    }

    public void Dispose()
    {
        foreach (var connection in _connections.Values)
        {
            connection.Dispose();
        }
    }
}