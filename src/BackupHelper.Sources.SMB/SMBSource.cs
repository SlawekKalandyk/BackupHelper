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
        var (shareInfo, connection, smbPath) = GetFullSMBInfo(path);
        return connection.GetStream(smbPath);
    }

    public IEnumerable<string> GetSubDirectories(string path)
    {
        var (shareInfo, connection, smbPath) = GetFullSMBInfo(path);
        return connection.GetSubDirectories(smbPath).Select(dir => Path.Join(shareInfo.ToString(), dir));
    }

    public IEnumerable<string> GetFiles(string path)
    {
        var (shareInfo, connection, smbPath) = GetFullSMBInfo(path);
        return connection.GetFiles(smbPath).Select(file => Path.Join(shareInfo.ToString(), file));
    }

    public bool FileExists(string path)
    {
        var (shareInfo, connection, smbPath) = GetFullSMBInfo(path);
        return connection.FileExists(smbPath);
    }

    public bool DirectoryExists(string path)
    {
        var (shareInfo , connection, smbPath) = GetFullSMBInfo(path);
        return connection.DirectoryExists(smbPath);
    }

    public DateTime? GetFileLastWriteTime(string path)
    {
        var (shareInfo , connection, smbPath) = GetFullSMBInfo(path);
        return connection.GetFileLastWriteTime(smbPath);
    }

    public DateTime? GetDirectoryLastWriteTime(string path)
    {
        var (shareInfo, connection, smbPath) = GetFullSMBInfo(path);
        return connection.GetDirectoryLastWriteTime(smbPath);
    }

    private (SMBShareInfo shareInfo, SMBConnection connection, string smbPath) GetFullSMBInfo(string path)
    {
        var shareInfo = SMBShareInfo.FromFilePath(path);
        var connection = GetConnection(shareInfo, path);
        var smbPath = SMBHelper.StripShareInfo(path);
        return (shareInfo, connection, smbPath);
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
        var credentialName = SMBCredentialHelper.GetSMBCredentialTitle(shareInfo.ServerIPAddress.ToString(), shareInfo.ShareName);
        var credential = _credentialsProvider.GetCredential(credentialName);

        if (credential == null)
        {
            throw new InvalidOperationException($"No credentials found for SMB share '{credentialName}'.");
        }

        return new SMBCredential(shareInfo.ServerIPAddress.ToString(), shareInfo.ShareName, credential.Username, credential.Password!);
    }

    public void Dispose()
    {
        foreach (var connection in _connections.Values)
        {
            connection.Dispose();
        }
    }
}