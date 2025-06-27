using System.Net;
using SMBLibrary;
using SMBLibrary.Client;
using FileAttributes = SMBLibrary.FileAttributes;

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
        ThrowIfStatusNotSuccess(status, nameof(_smb2Client.Login));

        _smbFileStore = _smb2Client.TreeConnect(shareName, out status);
        ThrowIfStatusNotSuccess(status, nameof(_smb2Client.TreeConnect));
    }

    public SMBConnection(IPAddress ipAddress, string domainName, string shareName, string username, string password)
    {
        _smb2Client = new SMB2Client();
        var connected = _smb2Client.Connect(ipAddress, SMBTransportType.DirectTCPTransport);
        if (!connected)
            throw new InvalidOperationException($"Failed to connect to SMB server at {ipAddress}");

        var status = _smb2Client.Login(domainName, username, password);
        ThrowIfStatusNotSuccess(status, nameof(_smb2Client.Login));

        _smbFileStore = _smb2Client.TreeConnect(shareName, out status);
        ThrowIfStatusNotSuccess(status, nameof(_smb2Client.TreeConnect));
    }

    public bool IsConnected => _smb2Client.IsConnected;

    public Stream GetStream(string filePath)
    {
        var fileHandle = GetFileFileHandle(filePath);
        var fileInfo = GetFileInfo(filePath, fileHandle);

        return new SMBReadOnlyFileStream(_smbFileStore, fileHandle, fileInfo.StandardInformation.EndOfFile);
    }

    public IEnumerable<string> GetSubDirectories(string directoryPath)
    {
        var fileHandle = GetDirectoryFileHandle(directoryPath);
        var fileInfos = GetFileInfos(directoryPath, fileHandle);
        return fileInfos
            .Where(info => (info.FileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
            .Select(info => Path.Combine(directoryPath, info.FileName));
    }

    public IEnumerable<string> GetFiles(string directoryPath)
    {
        var fileHandle = GetDirectoryFileHandle(directoryPath);
        var fileInfos = GetFileInfos(directoryPath, fileHandle);
        return fileInfos
            .Where(info => (info.FileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
            .Select(info => Path.Combine(directoryPath, info.FileName));
    }

    private IEnumerable<FileFullDirectoryInformation> GetFileInfos(string directoryPath, object fileHandle)
    {
        var status = _smbFileStore.QueryDirectory(
            out var fileInfoList,
            fileHandle,
            "*",
            FileInformationClass.FileFullDirectoryInformation);
        ThrowIfStatusNotNoMoreFiles(status, nameof(_smbFileStore.QueryDirectory));

        var fileInfoCount = fileInfoList.Count;
        var castedFileInfoList = fileInfoList.Cast<FileFullDirectoryInformation>().ToList();
        var castedFileInfoCount = castedFileInfoList.Count;
        if (fileInfoCount != castedFileInfoCount)
        {
            throw new InvalidOperationException($"Mismatch in file information count: expected {fileInfoCount}, got {castedFileInfoCount}");
        }

        return RemoveDotDirectories(castedFileInfoList);
    }

    private FileAllInformation GetFileInfo(string filePath, object fileHandle)
    {
        var status = _smbFileStore.GetFileInformation(
            out var fileInfo,
            fileHandle,
            FileInformationClass.FileAllInformation);
        ThrowIfStatusNotSuccess(status, nameof(_smbFileStore.GetFileInformation));

        if (fileInfo is not FileAllInformation fullFileInfo)
        {
            throw new InvalidOperationException($"Failed to retrieve file information for '{filePath}'");
        }

        return fullFileInfo;
    }

    /// <returns>fileHandle</returns>
    private object GetFileFileHandle(string filePath)
    {
        var status = _smbFileStore.CreateFile(
            out var fileHandle,
            out var fileStatus,
            filePath,
            AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE,
            FileAttributes.Normal,
            ShareAccess.Read,
            CreateDisposition.FILE_OPEN,
            CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_NONALERT,
            null);
        ThrowIfStatusNotSuccess(status, nameof(_smbFileStore.CreateFile));

        if (fileStatus != FileStatus.FILE_OPENED)
            throw new InvalidOperationException($"Failed to open file '{filePath}' with status: {fileStatus}");

        return fileHandle;
    }

    /// <returns>fileHandle</returns>
    private object GetDirectoryFileHandle(string directoryPath)
    {
        var status = _smbFileStore.CreateFile(
            out var fileHandle,
            out var fileStatus,
            directoryPath,
            AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE,
            FileAttributes.Directory,
            ShareAccess.Read,
            CreateDisposition.FILE_OPEN,
            CreateOptions.FILE_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_NONALERT,
            null);
        ThrowIfStatusNotSuccess(status, nameof(_smbFileStore.CreateFile));

        if (fileStatus != FileStatus.FILE_OPENED)
            throw new InvalidOperationException($"Failed to open directory '{directoryPath}' with status: {fileStatus}");

        return fileHandle;
    }

    private void ThrowIfStatusNotSuccess(NTStatus status, string operation)
    {
        if (status != NTStatus.STATUS_SUCCESS)
        {
            throw new InvalidOperationException($"SMB operation '{operation}' failed with status: {status}");
        }
    }

    private void ThrowIfStatusNotNoMoreFiles(NTStatus status, string operation)
    {
        if (status != NTStatus.STATUS_NO_MORE_FILES)
        {
            throw new InvalidOperationException($"SMB operation '{operation}' failed with status: {status}");
        }
    }

    private List<FileFullDirectoryInformation> RemoveDotDirectories(List<FileFullDirectoryInformation> fileInfoList)
    {
        return fileInfoList
            .Where(info => info.FileName != "." && info.FileName != "..")
            .ToList();
    }

    public void Dispose()
    {
        _smbFileStore.Disconnect();
        _smb2Client.Logoff();
        _smb2Client.Disconnect();
    }
}