using SMBLibrary;
using SMBLibrary.Client;
using FileAttributes = SMBLibrary.FileAttributes;

namespace BackupHelper.Sources.SMB;

public class SMBFile : SMBFileSystemComponentBase
{
    private SMBFile(ISMBFileStore smbFileStore, object fileHandle, FileAllInformation fileInfo, FilePurpose filePurpose)
        : base(smbFileStore, fileHandle, filePurpose)
    {
        FileInfo = fileInfo;
    }

    public FileAllInformation FileInfo { get; }

    public Stream GetStream()
    {
        if (FilePurpose != FilePurpose.Read)
            throw new InvalidOperationException("This file cannot be read. It was not opened for reading.");

        return new SMBReadOnlyFileStream(SMBFileStore, this);
    }

    public static SMBFile OpenFileForReading(ISMBFileStore fileStore, string filePath)
    {
        var status = fileStore.CreateFile(
            out var fileHandle,
            out var fileStatus,
            filePath,
            AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE,
            FileAttributes.Normal,
            ShareAccess.Read,
            CreateDisposition.FILE_OPEN,
            CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_NONALERT,
            null);
        SMBHelper.ThrowIfStatusNotSuccess(status, nameof(fileStore.CreateFile));

        if (fileStatus != FileStatus.FILE_OPENED)
            throw new InvalidOperationException($"Failed to open file '{filePath}' with status: {fileStatus}");

        var fileInfo = GetFileInfo(fileStore, filePath, fileHandle);
        return new SMBFile(fileStore, fileHandle, fileInfo, FilePurpose.Read);
    }

    public static SMBFile OpenFileForDeletion(ISMBFileStore fileStore, string filePath)
    {
        var status = fileStore.CreateFile(
            out var fileHandle,
            out var fileStatus,
            filePath,
            AccessMask.DELETE | AccessMask.SYNCHRONIZE,
            FileAttributes.Normal,
            ShareAccess.None,
            CreateDisposition.FILE_OPEN,
            CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT,
            null);
        SMBHelper.ThrowIfStatusNotSuccess(status, nameof(fileStore.CreateFile));

        if (fileStatus != FileStatus.FILE_OPENED)
            throw new InvalidOperationException($"Failed to open file '{filePath}' with status: {fileStatus}");

        var fileInfo = GetFileInfo(fileStore, filePath, fileHandle);
        return new SMBFile(fileStore, fileHandle, fileInfo, FilePurpose.Delete);
    }

    private static FileAllInformation GetFileInfo(ISMBFileStore fileStore, string filePath, object fileHandle)
    {
        var status = fileStore.GetFileInformation(
            out var fileInfo,
            fileHandle,
            FileInformationClass.FileAllInformation);
        SMBHelper.ThrowIfStatusNotSuccess(status, nameof(fileStore.GetFileInformation));

        if (fileInfo is not FileAllInformation fullFileInfo)
        {
            throw new InvalidOperationException($"Failed to retrieve file information for '{filePath}'");
        }

        return fullFileInfo;
    }
}