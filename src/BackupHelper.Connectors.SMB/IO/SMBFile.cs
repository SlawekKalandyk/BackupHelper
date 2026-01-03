using SMBLibrary;
using SMBLibrary.Client;
using FileAttributes = SMBLibrary.FileAttributes;

namespace BackupHelper.Connectors.SMB.IO;

public class SMBFile : SMBIOComponentBase
{
    private SMBFile(
        ISMBFileStore smbFileStore,
        object fileHandle,
        FileAllInformation fileInfo,
        FilePurpose filePurpose
    )
        : base(smbFileStore, fileHandle, filePurpose)
    {
        FileInfo = fileInfo;
    }

    public FileAllInformation FileInfo { get; }

    public Stream GetReadStream()
    {
        if ((FilePurpose & FilePurpose.Read) != FilePurpose.Read)
            throw new InvalidOperationException(
                "This file cannot be read. It was not opened for reading."
            );

        return new SMBReadOnlyFileStream(SMBFileStore, this);
    }

    public Stream GetWriteStream()
    {
        if ((FilePurpose & FilePurpose.Write) != FilePurpose.Write)
            throw new InvalidOperationException(
                "This file cannot be written to. It was not opened for writing."
            );

        return new SMBWriteOnlyFileStream(SMBFileStore, this);
    }

    public void Delete()
    {
        if ((FilePurpose & FilePurpose.Delete) != FilePurpose.Delete)
            throw new InvalidOperationException(
                "This file cannot be deleted. It was not opened for deletion."
            );

        var fileDispositionInformation = new FileDispositionInformation { DeletePending = true };
        var status = SMBFileStore.SetFileInformation(Handle, fileDispositionInformation);
        SMBHelper.ThrowIfStatusNotSuccess(status, nameof(SMBFileStore.SetFileInformation));
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
            null
        );
        SMBHelper.ThrowIfStatusNotSuccess(status, nameof(fileStore.CreateFile));
        SMBHelper.ThrowIfFileStatusNotFileOpened(
            fileStatus,
            filePath,
            nameof(fileStore.CreateFile)
        );

        var fileInfo = GetFileInfo(fileStore, filePath, fileHandle);
        return new SMBFile(fileStore, fileHandle, fileInfo, FilePurpose.Read);
    }

    public static SMBFile OpenFileForWriting(ISMBFileStore fileStore, string filePath)
    {
        var status = fileStore.CreateFile(
            out var fileHandle,
            out var fileStatus,
            filePath,
            AccessMask.GENERIC_READ | AccessMask.GENERIC_WRITE | AccessMask.SYNCHRONIZE,
            FileAttributes.Normal,
            ShareAccess.Read | ShareAccess.Write,
            CreateDisposition.FILE_OPEN,
            CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT,
            null
        );
        SMBHelper.ThrowIfStatusNotSuccess(status, nameof(fileStore.CreateFile));
        SMBHelper.ThrowIfFileStatusNotFileOpened(
            fileStatus,
            filePath,
            nameof(fileStore.CreateFile)
        );

        var fileInfo = GetFileInfo(fileStore, filePath, fileHandle);
        return new SMBFile(fileStore, fileHandle, fileInfo, FilePurpose.Read | FilePurpose.Write);
    }

    public static SMBFile OpenFileForDeletion(ISMBFileStore fileStore, string filePath)
    {
        var status = fileStore.CreateFile(
            out var fileHandle,
            out var fileStatus,
            filePath,
            AccessMask.GENERIC_READ | AccessMask.DELETE | AccessMask.SYNCHRONIZE,
            FileAttributes.Normal,
            ShareAccess.None,
            CreateDisposition.FILE_OPEN,
            CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT,
            null
        );
        SMBHelper.ThrowIfStatusNotSuccess(status, nameof(fileStore.CreateFile));
        SMBHelper.ThrowIfFileStatusNotFileOpened(
            fileStatus,
            filePath,
            nameof(fileStore.CreateFile)
        );

        var fileInfo = GetFileInfo(fileStore, filePath, fileHandle);
        return new SMBFile(fileStore, fileHandle, fileInfo, FilePurpose.Read | FilePurpose.Delete);
    }

    public static SMBFile CreateFile(ISMBFileStore fileStore, string filePath)
    {
        var status = fileStore.CreateFile(
            out var fileHandle,
            out var fileStatus,
            filePath,
            AccessMask.GENERIC_READ | AccessMask.GENERIC_WRITE | AccessMask.SYNCHRONIZE,
            FileAttributes.Normal,
            ShareAccess.Read | ShareAccess.Write,
            CreateDisposition.FILE_CREATE,
            CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_NONALERT,
            null
        );
        SMBHelper.ThrowIfStatusNotSuccess(status, nameof(fileStore.CreateFile));
        SMBHelper.ThrowIfFileStatusNotFileCreated(
            fileStatus,
            filePath,
            nameof(fileStore.CreateFile)
        );

        var fileInfo = GetFileInfo(fileStore, filePath, fileHandle);
        return new SMBFile(fileStore, fileHandle, fileInfo, FilePurpose.Read | FilePurpose.Write);
    }

    public static DateTime? GetLastWriteTime(ISMBFileStore fileStore, string filePath)
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
            null
        );
        SMBHelper.ThrowIfStatusNotSuccess(status, nameof(fileStore.CreateFile));
        SMBHelper.ThrowIfFileStatusNotFileOpened(
            fileStatus,
            filePath,
            nameof(fileStore.CreateFile)
        );

        var fileInfo = GetFileInfo(fileStore, filePath, fileHandle);
        fileStore.CloseFile(fileHandle);

        return fileInfo.BasicInformation.LastWriteTime.Time;
    }

    public static bool Exists(ISMBFileStore fileStore, string filePath)
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
            null
        );

        var fileExists = status == NTStatus.STATUS_SUCCESS && fileStatus == FileStatus.FILE_OPENED;

        if (fileExists)
            fileStore.CloseFile(fileHandle);

        return fileExists;
    }

    public static long GetFileSize(ISMBFileStore fileStore, string filePath)
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
            null
        );
        SMBHelper.ThrowIfStatusNotSuccess(status, nameof(fileStore.CreateFile));
        SMBHelper.ThrowIfFileStatusNotFileOpened(
            fileStatus,
            filePath,
            nameof(fileStore.CreateFile)
        );

        var fileInfo = GetFileInfo(fileStore, filePath, fileHandle);
        fileStore.CloseFile(fileHandle);

        return fileInfo.StandardInformation.EndOfFile;
    }

    private static FileAllInformation GetFileInfo(
        ISMBFileStore fileStore,
        string filePath,
        object fileHandle
    )
    {
        var status = fileStore.GetFileInformation(
            out var fileInfo,
            fileHandle,
            FileInformationClass.FileAllInformation
        );
        SMBHelper.ThrowIfStatusNotSuccess(status, nameof(fileStore.GetFileInformation));

        if (fileInfo is not FileAllInformation fullFileInfo)
        {
            throw new InvalidOperationException(
                $"Failed to retrieve file information for '{filePath}'"
            );
        }

        return fullFileInfo;
    }
}
