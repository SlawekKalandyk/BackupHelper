using System.Net;
using BackupHelper.Sources.SMB.IO;
using SMBLibrary;
using SMBLibrary.Client;

namespace BackupHelper.Sources.SMB;

public class SMBConnection : IDisposable
{
    private readonly SMB2Client _smb2Client;
    private readonly ISMBFileStore _smbFileStore;

    public SMBConnection(string serverName, string domainName, string shareName, string username, string password)
    {
        _smb2Client = new SMB2Client();
        _smb2Client.Connect(serverName, SMBTransportType.DirectTCPTransport);
        var status = _smb2Client.Login(domainName, username, password);
        SMBHelper.ThrowIfStatusNotSuccess(status, nameof(_smb2Client.Login));

        _smbFileStore = _smb2Client.TreeConnect(shareName, out status);
        SMBHelper.ThrowIfStatusNotSuccess(status, nameof(_smb2Client.TreeConnect));
    }

    public SMBConnection(IPAddress ipAddress, string domainName, string shareName, string username, string password)
    {
        _smb2Client = new SMB2Client();
        var connected = _smb2Client.Connect(ipAddress, SMBTransportType.DirectTCPTransport);

        if (!connected)
            throw new InvalidOperationException($"Failed to connect to SMB server at {ipAddress}");

        var status = _smb2Client.Login(domainName, username, password);
        SMBHelper.ThrowIfStatusNotSuccess(status, nameof(_smb2Client.Login));

        _smbFileStore = _smb2Client.TreeConnect(shareName, out status);
        SMBHelper.ThrowIfStatusNotSuccess(status, nameof(_smb2Client.TreeConnect));
    }

    public bool IsConnected => _smb2Client.IsConnected;

    public Stream GetStream(string filePath)
    {
        return new SMBReadOnlyFileStream(_smbFileStore, filePath);
    }

    public IEnumerable<string> GetSubDirectories(string directoryPath)
    {
        using var smbDirectory = SMBDirectory.OpenDirectoryForReading(_smbFileStore, directoryPath);
        return smbDirectory.GetSubDirectories();
    }

    public IEnumerable<string> GetFiles(string directoryPath)
    {
        using var smbDirectory = SMBDirectory.OpenDirectoryForReading(_smbFileStore, directoryPath);
        return smbDirectory.GetFiles();
    }

    public void ClearDirectory(string directoryPath)
    {
        using var smbDirectory = SMBDirectory.OpenDirectoryForReading(_smbFileStore, directoryPath);
        smbDirectory.ClearDirectory();
    }

    public void CreateDirectory(string directoryPath)
    {
        using var _ = SMBDirectory.CreateDirectory(_smbFileStore, directoryPath);
    }

    public void DeleteDirectory(string directoryPath)
    {
        using var smbDirectory = SMBDirectory.OpenDirectoryForDeletion(_smbFileStore, directoryPath);
        smbDirectory.Delete(recursive: true);
    }

    public Stream CreateFile(string filePath)
    {
        using (var _ = SMBFile.CreateFile(_smbFileStore, filePath))
        {
        }
        return new SMBWriteOnlyFileStream(_smbFileStore, filePath);
    }

    public void DeleteFile(string filePath)
    {
        using var smbFile = SMBFile.OpenFileForDeletion(_smbFileStore, filePath);
        smbFile.Delete();
    }

    public void Dispose()
    {
        _smbFileStore.Disconnect();
        _smb2Client.Logoff();
        _smb2Client.Disconnect();
    }
}